using System;
using DG.Tweening;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Data.Configs;
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
        private readonly Tween _tickTween;

        private float _slotAngle = 45f;
        private int _lastTickIndex = int.MinValue;

        public WheelPresenter(WheelView view, WheelSpinConfig spinConfig, RewardCatalog catalog, Sprite bombIcon)
        {
            _view = view;
            _spinConfig = spinConfig;
            _catalog = catalog;
            _bombIcon = bombIcon;

            // The editor's [ContextMenu] WheelSlotLayout tool is design-time-only convenience; nothing
            // guarantees a human ever ran it. Doing the same placement here means a fresh Play session is
            // correct regardless — otherwise every slot starts stacked at the rotor's centre instead of
            // over its painted hole in the base art, which is indistinguishable from "no icon at all".
            LayoutSlots();

            // Built once and restarted per tick rather than fired fresh each time: ~45 ticks happen over one
            // spin, and a prebuilt, paused, non-autokilled tween is the zero-alloc way to replay that.
            _tickTween = _view.Indicator
                .DOPunchRotation(new Vector3(0f, 0f, -_spinConfig.TickPunchDegrees), 0.09f, 1, 0f)
                .SetAutoKill(false)
                .Pause();
        }

        public void WireInput(GameStateMachine machine) => _view.SpinClicked += machine.RequestSpin;

        public void SetTheme(WheelModel wheel, WheelThemeConfig theme)
        {
            if (theme != null)
                _view.SetTheme(theme.BaseSprite, theme.IndicatorSprite, theme.AccentColor, theme.GlowColor);

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
                // A degenerate rect here collapses every slot's radius to ~0, stacking all eight on the
                // rotor's centre — indistinguishable from "no icon at all" and easy to mistake for a
                // population bug. Falling back to the design size keeps the wheel correct either way; the
                // warning makes it visible if this path is ever actually taken instead of silently masking it.
                Debug.LogWarning(
                    $"[Vertigo] WheelPresenter: rotor rect width was {wheelSize:F1}px when laying out slots; " +
                    $"falling back to the {DesignWheelSize:F0}px design size.");
                wheelSize = DesignWheelSize;
            }

            float radius = 0.315f * wheelSize;
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

        public void SetInteractable(bool interactable) => _view.SetSpinInteractable(interactable);

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
        }
    }
}
