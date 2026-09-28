using System;
using System.Collections.Generic;
using DG.Tweening;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The single <see cref="IWheelPresentation"/> the state machine talks to, composed from one small
    /// presenter per screen region. Core never sees any of the classes this delegates to.
    /// </summary>
    public sealed class ScreenPresentation : IWheelPresentation, IDisposable
    {
        private readonly WheelPresenter _wheel;
        private readonly ZoneMapPresenter _zoneMap;
        private readonly BankPresenter _bank;
        private readonly ActionBarPresenter _actionBar;
        private readonly PopupPresenter _popups;
        private readonly VfxPresenter _vfx;
        private readonly AudioPresenter _audio;
        private readonly ZoneProgressionConfig _progression;
        private readonly JuiceConfig _juice;

        // The bomb-impact hold has no view to SetLink to, so it is killed before a new one starts and on Dispose.
        private Tween _bombDelay;

        public ScreenPresentation(
            WheelPresenter wheel, ZoneMapPresenter zoneMap, BankPresenter bank,
            ActionBarPresenter actionBar, PopupPresenter popups, VfxPresenter vfx, AudioPresenter audio,
            ZoneProgressionConfig progression, JuiceConfig juice)
        {
            _wheel = wheel;
            _zoneMap = zoneMap;
            _bank = bank;
            _actionBar = actionBar;
            _popups = popups;
            _vfx = vfx;
            _audio = audio;
            _progression = progression;
            _juice = juice;
        }

        #region Zone and spin
        public void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete)
        {
            _bank.Refresh();

            // The wheel exits, re-themes off-screen and returns; then the strip scrolls and the flow reaches Idle.
            _audio.PlayWheelTransition();
            _wheel.PlayZoneTransition(
                wheel, _progression.ThemeFor(zone, zoneType), () => _zoneMap.ShowZone(zone, onComplete));
        }

        public void SetInputState(InputState state)
        {
            _wheel.SetInteractable(state.CanSpin);
            _actionBar.SetInputState(state.CanLeave);
        }

        public void PlaySpin(int slotIndex, Action onComplete)
        {
            _wheel.PlaySpin(slotIndex, onComplete);
        }

        public void PlayReveal(SpinOutcome outcome, ZoneType zoneType, Action onComplete)
        {
            // Fire-and-forget alongside the highlight: the chime plays on every landing, the glow burst on notable ones.
            if (!outcome.IsBomb)
            {
                _audio.PlayReward();
                if (zoneType != ZoneType.Normal)
                    _vfx.PlayRewardBurst();
            }

            _wheel.HighlightSlot(outcome.SlotIndex, onComplete);
        }

        public void PlayRewardGranted(SpinOutcome outcome, Action onComplete)
        {
            _bank.FlyIn(outcome, _wheel.SlotWorldPosition(outcome.SlotIndex), onComplete);
        }

        public void PlayBomb(Action onComplete)
        {
            _vfx.PlayBombImpact();
            _audio.PlayBombImpact();
            // The bank stays as it was behind the vignette; HideGameOver refreshes it once the player chooses.
            _bombDelay?.Kill();

            // Dropped on kill: with recycling on, a stale reference would kill whichever tween reuses the object.
            Tween delay = DOVirtual.DelayedCall(_juice.BombImpactHoldDuration, () => onComplete());
            delay.OnKill(() => { if (_bombDelay == delay) _bombDelay = null; });
            _bombDelay = delay;
        }
        #endregion

        #region Popups
        public void ShowGameOver(GameOverSummary summary)
        {
            _popups.ShowGameOver(summary);
        }

        public void HideGameOver()
        {
            // A revive restores the haul and a give-up wipes it; either way no ShowZone runs to refresh the bank.
            _bank.Refresh();
            _popups.HideGameOver();
        }

        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared, WalletBalances wallet)
        {
            _popups.ShowCashOut(haul, zonesCleared, wallet);
        }

        public void HideCashOut()
        {
            _popups.HideCashOut();
        }

        public void ClaimCashOut(WalletBalances wallet, Action onComplete)
        {
            _popups.ClaimCashOut(wallet, onComplete);
        }
        #endregion

        #region Lifetime
        public void Dispose()
        {
            _bombDelay?.Kill();
        }
        #endregion
    }
}
