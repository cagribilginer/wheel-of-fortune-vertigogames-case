using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.Data.Services;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Drives the wheel: re-theming per zone, populating the eight slots from the resolved
    /// <see cref="WheelModel"/>, and the spin/tick/reveal tweens.
    /// <para>
    /// The landing math is the one from the architecture plan verbatim: an absolute end rotation built from
    /// <see cref="Mathf.Repeat"/> plus whole turns, played with <see cref="RotateMode.FastBeyond360"/> — the
    /// only mode that both guarantees a forward-only arc and supports multiple turns without the float error
    /// <see cref="RotateMode.LocalAxisAdd"/> accumulates.
    /// </para>
    /// </summary>
    public sealed class WheelPresenter
    {
        private readonly WheelView _view;
        private readonly WheelSpinConfig _spinConfig;
        private readonly RewardCatalog _catalog;
        private readonly Sprite _bombIcon;
        private readonly IAudioService _audio;
        private readonly Tween _tickTween;
        private readonly Tween _breatheTween;

        private float _slotAngle = 45f;
        private int _lastTickIndex = int.MinValue;
        private AudioClip _tickClip;

        // The panel's resting Y and the fully-off-screen Y the wheel exits to between zones. Captured once
        // from the authored layout so an art/anchor change carries through without a magic number here.
        private readonly float _homeY;
        private readonly float _hiddenY;
        private bool _hasShownZone;
        private bool _slotsLaidOut;

        public WheelPresenter(
            WheelView view, WheelSpinConfig spinConfig, RewardCatalog catalog, Sprite bombIcon, IAudioService audio)
        {
            _view = view;
            _spinConfig = spinConfig;
            _catalog = catalog;
            _bombIcon = bombIcon;
            _audio = audio;

            _homeY = _view.Root.anchoredPosition.y;
            _hiddenY = _homeY - 900f; // a 720px panel plus margin: clears the bottom of the safe area entirely

            // Slot placement is deliberately NOT done here. The constructor runs inside GameInstaller.Awake,
            // before the Canvas has completed its first layout pass, so the rotor's rect still reads 0 wide.
            // It is done instead on the first SetTheme (the zone-setup cinematic), by which point the layout
            // has settled — see LayoutSlots.

            // Built once and restarted per tick rather than fired fresh each time: ~45 ticks happen over one
            // spin, and a prebuilt, paused, non-autokilled tween is the zero-alloc way to replay that.
            _tickTween = _view.Indicator
                .DOPunchRotation(new Vector3(0f, 0f, -_spinConfig.TickPunchDegrees), 0.09f, 1, 0f)
                .SetAutoKill(false)
                .Pause();

            // Targets the spin button's own rect, not its "_anim" child — that child is UIButtonPunch's
            // target, so the idle-breathe loop and a click's punch tween never fight over one transform's
            // localScale. Same prebuilt/restarted shape as the tick tween above, for the same reason: this
            // plays continuously while idle rather than firing once.
            _breatheTween = _view.SpinButtonRect
                .DOScale(1.04f, 1.1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetAutoKill(false)
                .Pause();
        }

        public void WireInput(GameStateMachine machine) => _view.SpinClicked += machine.RequestSpin;

        /// <summary>
        /// The zone-advance cinematic: drop the current wheel off the bottom, swap in the new zone's
        /// theme and slots while it is out of sight, then spring the fresh wheel back up to centre.
        /// <para>
        /// The very first zone has no outgoing wheel, so it only plays the entrance — which also keeps the
        /// boot-to-Idle chain inside the Play Mode smoke test's two-second budget.
        /// </para>
        /// </summary>
        public void PlayZoneTransition(WheelModel wheel, WheelThemeConfig theme, Action onComplete)
        {
            RectTransform root = _view.Root;
            root.DOKill();

            Sequence seq = DOTween.Sequence().SetLink(root.gameObject, LinkBehaviour.KillOnDestroy);

            if (_hasShownZone)
                seq.Append(root.DOAnchorPosY(_hiddenY, 0.35f).SetEase(Ease.InBack));
            else
                root.anchoredPosition = new Vector2(root.anchoredPosition.x, _hiddenY);

            seq.AppendCallback(() => SetTheme(wheel, theme));
            seq.Append(root.DOAnchorPosY(_homeY, 0.45f).SetEase(Ease.OutBack));
            seq.OnComplete(() =>
            {
                _hasShownZone = true;
                onComplete();
            });
        }

        public void SetTheme(WheelModel wheel, WheelThemeConfig theme)
        {
            if (theme != null)
            {
                _view.SetTheme(theme.BaseSprite, theme.IndicatorSprite, theme.AccentColor, theme.GlowColor);
                _tickClip = theme.Tick;
            }

            // First zone setup: the Canvas has laid out by now, so the rotor rect is real. Placing the
            // slots here rather than in the constructor is the whole fix for the "rect width was 0" path.
            if (!_slotsLaidOut) LayoutSlots();

            PopulateSlots(wheel);
        }

        // The wheel panel's authored, fixed design size (see MainSceneBuilder.BuildWheel) — the fallback
        // LayoutSlots reaches for if the rotor's rect ever reads back degenerate.
        private const float DesignWheelSize = 720f;

        private void LayoutSlots()
        {
            if (_view.Rotor == null || _view.Slots.Count == 0) return;

            float wheelSize = _view.Rotor.rect.width;
            if (wheelSize < 50f)
            {
                // Called before the rotor's rect resolved (an AspectRatioFitter drives it). Flush the
                // pending layout and re-read before deciding anything.
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Root);
                wheelSize = _view.Rotor.rect.width;
            }

            if (wheelSize < 50f)
            {
                // Still degenerate: fall back to the authored fixed size (MainSceneBuilder.BuildWheel pins
                // the panel to 720x720). A degenerate rect would otherwise collapse every slot's radius to
                // ~0 and stack all eight on the rotor centre. Silent because the fallback value is the
                // correct one; a stacked-slot bug would show up in the Game view immediately anyway.
                wheelSize = DesignWheelSize;
            }
            else
            {
                _slotsLaidOut = true;
            }

            // 0.303 puts a slot's centre (icons are centred in the slot, no local offset) dead in the middle
            // of the bronze cylinder holes on the 720px base art — 0.315 sat them a touch proud of the rim.
            float radius = 0.303f * wheelSize;
            float slotAngle = 360f / _view.Slots.Count;

            for (int i = 0; i < _view.Slots.Count; i++)
            {
                float angleDeg = i * slotAngle;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float x = radius * Mathf.Sin(angleRad);
                float y = radius * Mathf.Cos(angleRad);

                RectTransform slot = _view.Slots[i].Rect;
                slot.anchoredPosition = new Vector2(x, y);

                // Cancels the slot's own position angle so its local "up" always points radially outward —
                // i.e. the bottom of the icon/text faces the hub. This holds throughout any later rotor
                // spin too: rotating the rotor is a rigid transform, so a slot correct at rest stays
                // correct at every intermediate angle, not just when the wheel is standing still.
                slot.localEulerAngles = new Vector3(0f, 0f, -angleDeg);
            }
        }

        private void PopulateSlots(WheelModel wheel)
        {
            _slotAngle = 360f / wheel.SliceCount;

            for (int i = 0; i < wheel.SliceCount && i < _view.Slots.Count; i++)
            {
                WheelSlice slice = wheel[i];
                if (slice.IsBomb)
                {
                    _view.Slots[i].SetBomb(_bombIcon);
                    continue;
                }

                Sprite icon = _catalog.IconFor(slice.Reward);
                if (icon == null)
                {
                    Debug.LogWarning(
                        $"[Vertigo] WheelPresenter: RewardCatalog has no icon for '{slice.Reward}' " +
                        $"(slot {i}) — check the RewardDefinition asset's Icon field.");
                }
                _view.Slots[i].SetReward(icon, slice.Amount);
            }
        }

        public void SetInteractable(bool interactable)
        {
            _view.SetSpinInteractable(interactable);

            if (interactable)
            {
                _breatheTween.Restart();
            }
            else
            {
                _breatheTween.Pause();
                _view.SpinButtonRect.localScale = Vector3.one;
            }
        }

        public Vector3 SlotWorldPosition(int slotIndex) => _view.Slots[slotIndex].Rect.position;

        public void PlaySpin(int slotIndex, Action onComplete)
        {
            _lastTickIndex = int.MinValue;

            // Unity's positive Z rotation is counter-clockwise on screen, but slot index increases
            // clockwise (LayoutSlots' x = R*sin, y = R*cos). Rotating the rotor CCW by a slot's own
            // clockwise angle is exactly what cancels that angle out and brings it to the top — the
            // negated form previously here rotated the wrong way and landed the mirror-image slot instead.
            float targetLocal = slotIndex * _slotAngle;
            float current = _view.Rotor.localEulerAngles.z;
            float delta = Mathf.Repeat(targetLocal - current, 360f);
            int turns = UnityEngine.Random.Range(_spinConfig.MinTurns, _spinConfig.MaxTurns + 1);
            float endValue = current + delta + turns * 360f;

            _view.Rotor.DOKill();
            _view.Rotor
                .DOLocalRotate(new Vector3(0f, 0f, endValue), _spinConfig.Duration, RotateMode.FastBeyond360)
                .SetEase(_spinConfig.SpinEase)
                .SetLink(_view.Rotor.gameObject, LinkBehaviour.KillOnDestroy)
                .OnUpdate(EmitTicks)
                .OnComplete(() =>
                {
                    _view.Rotor.DOPunchRotation(new Vector3(0f, 0f, _spinConfig.SettlePunchDegrees), 0.28f, 6, 1f);
                    onComplete();
                });
        }

        public void HighlightSlot(int slotIndex, Action onComplete)
        {
            if (slotIndex < 0 || slotIndex >= _view.Slots.Count) { onComplete(); return; }

            _view.Slots[slotIndex].Rect.DOKill();
            _view.Slots[slotIndex].Rect
                .DOScale(1.25f, 0.18f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => onComplete());
        }

        private void EmitTicks()
        {
            // Same sign as targetLocal above, for the same reason: the rotor's rotation directly equals
            // the clockwise angle of whichever slot currently sits at the top.
            int idx = (int)(_view.Rotor.localEulerAngles.z / _slotAngle);
            if (idx == _lastTickIndex) return;

            _lastTickIndex = idx;
            _tickTween.Restart();
            _audio.PlayOneShot(_tickClip, 0.5f);
        }
    }
}
