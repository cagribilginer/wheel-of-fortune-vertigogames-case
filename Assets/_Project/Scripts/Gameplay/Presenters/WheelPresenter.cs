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
    /// Drives the wheel: per-zone theming, the eight slots, and the spin, tick and reveal tweens. The spin is an
    /// absolute end rotation played with <see cref="RotateMode.FastBeyond360"/>, the one mode that guarantees a
    /// forward arc over several turns without float drift.
    /// </summary>
    public sealed class WheelPresenter : IDisposable
    {
        // Below this a wheel rect is treated as "not laid out yet"; a real one is hundreds of units wide.
        private const float MIN_LAID_OUT_WHEEL_SIZE = 50f;
        private const float TICK_VOLUME = 0.5f;

        private readonly WheelView _view;
        private readonly WheelSpinConfig _spinConfig;
        private readonly RewardCatalog _catalog;
        private readonly Sprite _bombIcon;
        private readonly IAudioService _audio;
        private readonly IRandomProvider _random;
        private readonly JuiceConfig _juice;
        private readonly Tween _tickTween;

        // The continuation of the one transition / spin / highlight in flight. The state machine runs them strictly
        // one at a time, so a field per kind is enough and each completion is a named handler, not a closure.
        private WheelModel _transitionWheel;
        private WheelThemeConfig _transitionTheme;
        private Action _onZoneEntered;
        private Action _onSpinStopped;
        private readonly Tween _breatheTween;

        private GameStateMachine _machine;
        private float _slotAngle = 360f / WheelModel.STANDARD_SLICE_COUNT;
        private int _lastTickIndex = int.MinValue;
        private AudioClip _tickClip;

        // The resting and fully-off-screen Y, captured once from the authored layout.
        private readonly float _homeY;
        private readonly float _hiddenY;
        private bool _hasShownZone;
        private bool _hasLaidOutSlots;

        public WheelPresenter(
            WheelView view, WheelSpinConfig spinConfig, RewardCatalog catalog, Sprite bombIcon,
            IAudioService audio, IRandomProvider random, JuiceConfig juice)
        {
            _view = view;
            _spinConfig = spinConfig;
            _catalog = catalog;
            _bombIcon = bombIcon;
            _audio = audio;
            _random = random;
            _juice = juice;

            _homeY = _view.Root.anchoredPosition.y;
            _hiddenY = _homeY - _juice.ZoneHiddenOffsetY;

            _tickTween = _view.Indicator
                .DOPunchRotation(new Vector3(0f, 0f, -_spinConfig.TickPunchDegrees), _juice.TickPunchDuration, 1, 0f)
                .SetAutoKill(false)
                .SetLink(_view.Indicator.gameObject, LinkBehaviour.KillOnDestroy)
                .Pause();

            _breatheTween = _view.SpinButtonRect
                .DOScale(_juice.BreatheScale, _juice.BreatheDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetAutoKill(false)
                .SetLink(_view.SpinButtonRect.gameObject, LinkBehaviour.KillOnDestroy)
                .Pause();
        }

        #region Input and lifetime
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
        #endregion

        #region Zone transition and theme
        /// <summary>
        /// The zone-advance cinematic: the old wheel drops off the bottom, the new theme and slots swap in unseen,
        /// then the wheel springs back. The first zone has no outgoing wheel and only plays the entrance.
        /// </summary>
        public void PlayZoneTransition(WheelModel wheel, WheelThemeConfig theme, Action onComplete)
        {
            RectTransform root = _view.Root;
            root.DOComplete();

            _transitionWheel = wheel;
            _transitionTheme = theme;
            _onZoneEntered = onComplete;

            Sequence seq = DOTween.Sequence().SetLink(root.gameObject, LinkBehaviour.KillOnDestroy);

            if (_hasShownZone)
                seq.Append(root.DOAnchorPosY(_hiddenY, _juice.ZoneExitDuration).SetEase(Ease.InBack));
            else
                root.anchoredPosition = new Vector2(root.anchoredPosition.x, _hiddenY);

            seq.AppendCallback(ApplyTransitionTheme);
            seq.Append(root.DOAnchorPosY(_homeY, _juice.ZoneEnterDuration).SetEase(Ease.OutBack));
            seq.OnComplete(OnZoneEntered);
        }

        private void ApplyTransitionTheme()
        {
            SetTheme(_transitionWheel, _transitionTheme);
        }

        private void OnZoneEntered()
        {
            _hasShownZone = true;
            _onZoneEntered();
        }

        public void SetTheme(WheelModel wheel, WheelThemeConfig theme)
        {
            if (theme)
            {
                _view.SetTheme(theme.BaseSprite, theme.IndicatorSprite, theme.AccentColor, theme.GlowColor);
                _tickClip = theme.Tick;
            }

            if (!_hasLaidOutSlots) LayoutSlots();

            PopulateSlots(wheel);
        }

        // The panel's authored size in Main.unity, the fallback if the rotor rect reads back degenerate.
        private const float DESIGN_WHEEL_SIZE = 720f;

        private void LayoutSlots()
        {
            if (!_view.Rotor || _view.Slots.Count == 0) return;

            float wheelSize = _view.Rotor.rect.width;
            if (wheelSize < MIN_LAID_OUT_WHEEL_SIZE)
            {
                // The rotor rect may not have resolved yet (an AspectRatioFitter drives it): flush the layout and re-read.
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Root);
                wheelSize = _view.Rotor.rect.width;
            }

            if (wheelSize < MIN_LAID_OUT_WHEEL_SIZE)
            {
                // Still degenerate: use the authored size rather than collapse every slot to the rotor centre.
                wheelSize = DESIGN_WHEEL_SIZE;
            }
            else
            {
                _hasLaidOutSlots = true;
            }

            _view.LayoutSlots(wheelSize);
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
                if (!icon)
                {
                    Debug.LogWarning(
                        $"[Vertigo] WheelPresenter: RewardCatalog has no icon for '{slice.Reward}' " +
                        $"(slot {i}) — check the RewardDefinition asset's Icon field.");
                }
                _view.Slots[i].SetReward(icon, slice.Amount);
            }
        }
        #endregion

        #region Wheel state
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
        #endregion

        #region Spin and ticks
        /// <summary>
        /// Turns the rotor to <paramref name="slotIndex"/>. Positive Z is counter-clockwise but slot index grows
        /// clockwise, so a turn by the slot's own angle brings that slot to the top.
        /// </summary>
        public void PlaySpin(int slotIndex, Action onComplete)
        {
            _lastTickIndex = int.MinValue;

            float targetLocal = slotIndex * _slotAngle;
            float current = _view.Rotor.localEulerAngles.z;
            float delta = Mathf.Repeat(targetLocal - current, 360f);
            int turns = _spinConfig.MinTurns + _random.Next(_spinConfig.MaxTurns - _spinConfig.MinTurns + 1);
            float endValue = current + delta + turns * 360f;

            _onSpinStopped = onComplete;

            _view.Rotor.DOComplete();
            _view.Rotor
                .DOLocalRotate(new Vector3(0f, 0f, endValue), _spinConfig.Duration, RotateMode.FastBeyond360)
                .SetEase(_spinConfig.SpinEase)
                .SetLink(_view.Rotor.gameObject, LinkBehaviour.KillOnDestroy)
                .OnUpdate(EmitTicks)
                .OnComplete(OnSpinTweenFinished);
        }

        private void OnSpinTweenFinished()
        {
            _view.Rotor.DOPunchRotation(
                    new Vector3(0f, 0f, _spinConfig.SettlePunchDegrees),
                    _juice.SettlePunchDuration, _juice.SettlePunchVibrato, _juice.SettlePunchElasticity)
                .SetLink(_view.Rotor.gameObject, LinkBehaviour.KillOnDestroy);
            _onSpinStopped();
        }

        public void HighlightSlot(int slotIndex, Action onComplete)
        {
            if (slotIndex < 0 || slotIndex >= _view.Slots.Count) { onComplete(); return; }

            RectTransform slotRect = _view.Slots[slotIndex].Rect;
            slotRect.DOComplete();
            slotRect
                .DOScale(_juice.HighlightScale, _juice.HighlightDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetLink(slotRect.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(new TweenCallback(onComplete));
        }

        private void EmitTicks()
        {
            // Same sign as targetLocal: the rotor angle equals the clockwise angle of the slot at the top.
            int idx = (int)(_view.Rotor.localEulerAngles.z / _slotAngle);
            if (idx == _lastTickIndex) return;

            _lastTickIndex = idx;
            _tickTween.Restart();
            _audio.PlayOneShot(_tickClip, TICK_VOLUME);
        }
        #endregion
    }
}
