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
    public sealed class WheelPresenter : IDisposable
    {
        private readonly WheelView _view;
        private readonly WheelSpinConfig _spinConfig;
        private readonly RewardCatalog _catalog;
        private readonly Sprite _bombIcon;
        private readonly IAudioService _audio;
        private readonly JuiceConfig _juice;
        private readonly Tween _tickTween;
        private readonly Tween _breatheTween;

        private GameStateMachine _machine;
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
            WheelView view, WheelSpinConfig spinConfig, RewardCatalog catalog, Sprite bombIcon,
            IAudioService audio, JuiceConfig juice)
        {
            _view = view;
            _spinConfig = spinConfig;
            _catalog = catalog;
            _bombIcon = bombIcon;
            _audio = audio;
            _juice = juice;

            _homeY = _view.Root.anchoredPosition.y;
            _hiddenY = _homeY - _juice.ZoneHiddenOffsetY;

            // Slot placement isn't done here: the Canvas hasn't laid out yet at construction time, so the
            // rotor's rect still reads 0 wide. It happens on the first SetTheme instead — see LayoutSlots.

            // Built once and restarted per tick rather than fired fresh each time: ~45 ticks happen over one
            // spin, and a prebuilt, paused, non-autokilled tween is the zero-alloc way to replay that.
            _tickTween = _view.Indicator
                .DOPunchRotation(new Vector3(0f, 0f, -_spinConfig.TickPunchDegrees), _juice.TickPunchDuration, 1, 0f)
                .SetAutoKill(false)
                .SetLink(_view.Indicator.gameObject, LinkBehaviour.KillOnDestroy)
                .Pause();

            // Targets the spin button's own rect, not its "_anim" child (UIButtonPunch's target), so the
            // idle-breathe loop and a click's punch tween never fight over one transform's localScale.
            _breatheTween = _view.SpinButtonRect
                .DOScale(_juice.BreatheScale, _juice.BreatheDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetAutoKill(false)
                .SetLink(_view.SpinButtonRect.gameObject, LinkBehaviour.KillOnDestroy)
                .Pause();
        }

        public void WireInput(GameStateMachine machine)
        {
            _machine = machine;
            _view.SpinClicked += machine.RequestSpin;
        }

        public void Dispose()
        {
            _tickTween.Kill();
            _breatheTween.Kill();

            if (_machine != null) _view.SpinClicked -= _machine.RequestSpin;
        }

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
                seq.Append(root.DOAnchorPosY(_hiddenY, _juice.ZoneExitDuration).SetEase(Ease.InBack));
            else
                root.anchoredPosition = new Vector2(root.anchoredPosition.x, _hiddenY);

            seq.AppendCallback(() => SetTheme(wheel, theme));
            seq.Append(root.DOAnchorPosY(_homeY, _juice.ZoneEnterDuration).SetEase(Ease.OutBack));
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
        private const float DESIGN_WHEEL_SIZE = 720f;

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
                // the panel to 720x720) rather than collapse every slot's radius to ~0 at the rotor centre.
                wheelSize = DESIGN_WHEEL_SIZE;
            }
            else
            {
                _slotsLaidOut = true;
            }

            float radius = WheelView.SLOT_RING_RADIUS * wheelSize;
            float slotAngle = 360f / _view.Slots.Count;

            for (int i = 0; i < _view.Slots.Count; i++)
            {
                float angleDeg = i * slotAngle;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float x = radius * Mathf.Sin(angleRad);
                float y = radius * Mathf.Cos(angleRad);

                RectTransform slot = _view.Slots[i].Rect;
                slot.anchoredPosition = new Vector2(x, y);

                // Cancels the slot's own position angle so its local "up" points radially outward — the
                // bottom of the icon/text faces the hub, and stays correct through any later rotor spin.
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

        public Vector3 SlotWorldPosition(int slotIndex)
        {
            return _view.Slots[slotIndex].Rect.position;
        }

        public void PlaySpin(int slotIndex, Action onComplete)
        {
            _lastTickIndex = int.MinValue;

            // Unity's positive Z rotation is CCW on screen, but slot index increases clockwise (LayoutSlots'
            // x = R*sin, y = R*cos), so rotating the rotor CCW by a slot's own clockwise angle brings it to the top.
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
                    _view.Rotor.DOPunchRotation(
                            new Vector3(0f, 0f, _spinConfig.SettlePunchDegrees),
                            _juice.SettlePunchDuration, _juice.SettlePunchVibrato, _juice.SettlePunchElasticity)
                        .SetLink(_view.Rotor.gameObject, LinkBehaviour.KillOnDestroy);
                    onComplete();
                });
        }

        public void HighlightSlot(int slotIndex, Action onComplete)
        {
            if (slotIndex < 0 || slotIndex >= _view.Slots.Count) { onComplete(); return; }

            RectTransform slotRect = _view.Slots[slotIndex].Rect;
            slotRect.DOKill();
            slotRect
                .DOScale(_juice.HighlightScale, _juice.HighlightDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(slotRect.gameObject, LinkBehaviour.KillOnDestroy)
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
