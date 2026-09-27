using System;
using System.Collections.Generic;
using DG.Tweening;
using Vertigo.Wheel.Core.Rewards;
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

        // The bomb-impact hold has no single view to SetLink to (it just delays onComplete), so its
        // lifetime is guaranteed by hand: killed before a new one starts, and on Dispose.
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

        public void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete)
        {
            _bank.Refresh();

            // The wheel exits downward, re-themes and re-populates its slots off-screen, then rides back
            // up — only then does the zone strip scroll and the flow reach Idle. One swoosh covers the
            // whole move, tier swaps included (a Bronze->Silver change always rides a zone transition).
            _audio.PlayWheelTransition();
            _wheel.PlayZoneTransition(
                wheel, _progression.ThemeFor(zone, zoneType), () => _zoneMap.ShowZone(zone, onComplete));
        }

        public void SetInputState(bool canSpin, bool canLeave)
        {
            _wheel.SetInteractable(canSpin);
            _actionBar.SetInputState(canLeave);
        }

        public void PlaySpin(int slotIndex, Action onComplete)
        {
            _wheel.PlaySpin(slotIndex, onComplete);
        }

        public void PlayReveal(SpinOutcome outcome, ZoneType zoneType, Action onComplete)
        {
            // Fire-and-forget: both play alongside the highlight tween, not gating onComplete, since
            // nothing downstream needs to wait on a purely cosmetic flourish. The chime plays on every
            // reward landing; the glow burst is reserved for the ones worth calling out visually.
            if (!outcome.IsBomb)
            {
                _audio.PlayReward();
                if (zoneType != ZoneType.Normal || outcome.UnitValue >= _juice.BigRewardUnitValue)
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
            // The bank panel is deliberately NOT refreshed here: the pre-bomb haul stays on screen behind
            // the defeat vignette so a revive restores it seamlessly. HideGameOver refreshes once the
            // player has actually chosen (revive keeps it, give-up/restart empties it).
            _bombDelay?.Kill();
            _bombDelay = DOVirtual.DelayedCall(_juice.BombImpactHoldDuration, () => onComplete());
        }

        public void ShowGameOver(GameOverSummary summary)
        {
            _popups.ShowGameOver(summary);
        }

        public void HideGameOver()
        {
            // A revive restored the haul and a give-up wiped it — either way the board the player returns to
            // needs the current bank, and no ShowZone runs on the revive path to do it.
            _bank.Refresh();
            _popups.HideGameOver();
        }

        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared, int playerGold, int playerCash)
        {
            _popups.ShowCashOut(haul, zonesCleared, playerGold, playerCash);
        }

        public void HideCashOut()
        {
            _popups.HideCashOut();
        }

        public void ClaimCashOut(int playerGold, int playerCash, Action onComplete)
        {
            _popups.ClaimCashOut(playerGold, playerCash, onComplete);
        }

        public void Dispose()
        {
            _bombDelay?.Kill();
        }
    }
}
