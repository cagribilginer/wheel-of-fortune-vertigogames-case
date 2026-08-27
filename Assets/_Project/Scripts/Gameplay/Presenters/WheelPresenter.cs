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
            _slotAngle = 360f / wheel.SliceCount;

            if (theme != null)
                _view.SetTheme(theme.BaseSprite, theme.IndicatorSprite, theme.AccentColor, theme.GlowColor);

            for (int i = 0; i < wheel.SliceCount && i < _view.Slots.Count; i++)
            {
                WheelSlice slice = wheel[i];
                if (slice.IsBomb) _view.Slots[i].SetBomb(_bombIcon);
                else _view.Slots[i].SetReward(_catalog.IconFor(slice.Reward), slice.Amount);
            }
        }

        public void SetInteractable(bool interactable) => _view.SetSpinInteractable(interactable);

        public Vector3 SlotWorldPosition(int slotIndex) => _view.Slots[slotIndex].Rect.position;

        public void PlaySpin(int slotIndex, Action onComplete)
        {
            _lastTickIndex = int.MinValue;

            float targetLocal = -slotIndex * _slotAngle;
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
            int idx = (int)(-_view.Rotor.localEulerAngles.z / _slotAngle);
            if (idx == _lastTickIndex) return;

            _lastTickIndex = idx;
            _tickTween.Restart();
        }
    }
}
