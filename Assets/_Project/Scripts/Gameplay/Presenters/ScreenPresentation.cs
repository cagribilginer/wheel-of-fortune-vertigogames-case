using System;
using System.Collections.Generic;
using DG.Tweening;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The single <see cref="IWheelPresentation"/> the state machine talks to, composed from one small
    /// presenter per screen region. Core never sees any of the classes this delegates to.
    /// </summary>
    public sealed class ScreenPresentation : IWheelPresentation
    {
        private readonly HeaderView _header;
        private readonly WheelPresenter _wheel;
        private readonly ZoneMapPresenter _zoneMap;
        private readonly BankPresenter _bank;
        private readonly ActionBarPresenter _actionBar;
        private readonly PopupPresenter _popups;
        private readonly VfxPresenter _vfx;
        private readonly AudioPresenter _audio;
        private readonly WheelThemeConfig _bronzeTheme;
        private readonly WheelThemeConfig _silverTheme;
        private readonly WheelThemeConfig _goldenTheme;

        // Fixed, zone-independent per-unit worth (RewardDefinition.EstimatedValue) — the same scale
        // PopupPresenter's chest tiers use — at or above which a landed reward earns the glow burst on top
        // of any safe/super zone clear it might also be.
        private const int BigRewardUnitValue = 60;

        public ScreenPresentation(
            HeaderView header, WheelPresenter wheel, ZoneMapPresenter zoneMap, BankPresenter bank,
            ActionBarPresenter actionBar, PopupPresenter popups, VfxPresenter vfx, AudioPresenter audio,
            WheelThemeConfig bronzeTheme, WheelThemeConfig silverTheme, WheelThemeConfig goldenTheme)
        {
            _header = header;
            _wheel = wheel;
            _zoneMap = zoneMap;
            _bank = bank;
            _actionBar = actionBar;
            _popups = popups;
            _vfx = vfx;
            _audio = audio;
            _bronzeTheme = bronzeTheme;
            _silverTheme = silverTheme;
            _goldenTheme = goldenTheme;
        }

        public void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete)
        {
            _bank.Refresh();

            // The header only exists for the end-of-run screens; the wheel loop stays uncluttered.
            _header.SetVisible(false);

            // The wheel exits downward, re-themes and re-populates its slots off-screen, then rides back
            // up — only then does the zone strip scroll and the flow reach Idle. One swoosh covers the
            // whole move, tier swaps included (a Bronze->Silver change always rides a zone transition).
            _audio.PlayWheelTransition();
            _wheel.PlayZoneTransition(
                wheel, ThemeFor(wheel.Tier), () => _zoneMap.ShowZone(zone, onComplete));
        }

        public void SetInputState(bool canSpin, bool canLeave, bool canGiveUp)
        {
            _wheel.SetInteractable(canSpin);
            _actionBar.SetInputState(canLeave, canGiveUp);
        }

        public void PlaySpin(int slotIndex, Action onComplete) => _wheel.PlaySpin(slotIndex, onComplete);

        public void PlayReveal(SpinOutcome outcome, ZoneType zoneType, Action onComplete)
        {
            // Fire-and-forget: both play alongside the highlight tween, not gating onComplete, since
            // nothing downstream needs to wait on a purely cosmetic flourish. The chime plays on every
            // reward landing; the glow burst is reserved for the ones worth calling out visually.
            if (!outcome.IsBomb)
            {
                _audio.PlayReward();
                if (zoneType != ZoneType.Normal || outcome.UnitValue >= BigRewardUnitValue)
                    _vfx.PlayRewardBurst();
            }

            _wheel.HighlightSlot(outcome.SlotIndex, onComplete);
        }

        public void PlayRewardGranted(SpinOutcome outcome, Action onComplete) =>
            _bank.FlyIn(outcome, _wheel.SlotWorldPosition(outcome.SlotIndex), onComplete);

        public void PlayBomb(Action onComplete)
        {
            _vfx.PlayBombImpact();
            _audio.PlayBombImpact();
            // The bank panel is deliberately NOT refreshed here: the pre-bomb haul stays on screen behind
            // the defeat vignette so a revive restores it seamlessly. HideGameOver refreshes once the
            // player has actually chosen (revive keeps it, give-up/restart empties it).
            DOVirtual.DelayedCall(0.4f, () => onComplete());
        }

        public void ShowGameOver(
            int zoneReached, IReadOnlyList<BankEntry> lostHaul, int playerGold,
            bool goldReviveOffered, int goldReviveCost, bool adReviveOffered)
        {
            _header.SetVisible(true);
            _popups.ShowGameOver(
                zoneReached, lostHaul, playerGold, goldReviveOffered, goldReviveCost, adReviveOffered);
        }

        public void HideGameOver()
        {
            _header.SetVisible(false);
            // A revive restored the haul and a give-up wiped it — either way the board the player returns to
            // needs the current bank, and no ShowZone runs on the revive path to do it.
            _bank.Refresh();
            _popups.HideGameOver();
        }

        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared)
        {
            _header.SetVisible(true);
            _popups.ShowCashOut(haul, zonesCleared);
        }

        public void HideCashOut()
        {
            _header.SetVisible(false);
            _popups.HideCashOut();
        }

        public void ClaimCashOut(Action onComplete)
        {
            // The header stays visible through the celebration so its gold counter can be seen counting
            // up; ShowZone hides it again once the fresh run's wheel slides in.
            _popups.ClaimCashOut(onComplete);
        }

        public void ShowGiveUpConfirm(int rewardsAtStake)
        {
            _header.SetVisible(true);
            _popups.ShowGiveUpConfirm(rewardsAtStake);
        }

        public void HideGiveUpConfirm()
        {
            _header.SetVisible(false);
            _popups.HideGiveUpConfirm();
        }

        private WheelThemeConfig ThemeFor(WheelTier tier)
        {
            switch (tier)
            {
                case WheelTier.Silver: return _silverTheme;
                case WheelTier.Golden: return _goldenTheme;
                default: return _bronzeTheme;
            }
        }
    }
}
