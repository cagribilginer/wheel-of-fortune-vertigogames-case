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
    public sealed class ScreenPresentation : IWheelPresentation
    {
        private readonly HeaderPresenter _header;
        private readonly WheelPresenter _wheel;
        private readonly ZoneMapPresenter _zoneMap;
        private readonly BankPresenter _bank;
        private readonly ActionBarPresenter _actionBar;
        private readonly PopupPresenter _popups;
        private readonly VfxPresenter _vfx;
        private readonly WheelThemeConfig _bronzeTheme;
        private readonly WheelThemeConfig _silverTheme;
        private readonly WheelThemeConfig _goldenTheme;

        // Fixed, zone-independent per-unit worth (RewardDefinition.EstimatedValue) — the same scale
        // PopupPresenter's chest tiers use — at or above which a landed reward earns the glow burst on top
        // of any safe/super zone clear it might also be.
        private const int BigRewardUnitValue = 60;

        public ScreenPresentation(
            HeaderPresenter header, WheelPresenter wheel, ZoneMapPresenter zoneMap, BankPresenter bank,
            ActionBarPresenter actionBar, PopupPresenter popups, VfxPresenter vfx,
            WheelThemeConfig bronzeTheme, WheelThemeConfig silverTheme, WheelThemeConfig goldenTheme)
        {
            _header = header;
            _wheel = wheel;
            _zoneMap = zoneMap;
            _bank = bank;
            _actionBar = actionBar;
            _popups = popups;
            _vfx = vfx;
            _bronzeTheme = bronzeTheme;
            _silverTheme = silverTheme;
            _goldenTheme = goldenTheme;
        }

        public void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete)
        {
            _header.SetZone(zone);
            _wheel.SetTheme(wheel, ThemeFor(wheel.Tier));
            _bank.Refresh();
            _zoneMap.ShowZone(zone, onComplete);
        }

        public void SetInputState(bool canSpin, bool canLeave, bool canGiveUp)
        {
            _wheel.SetInteractable(canSpin);
            _actionBar.SetInputState(canLeave, canGiveUp);
        }

        public void PlaySpin(int slotIndex, Action onComplete) => _wheel.PlaySpin(slotIndex, onComplete);

        public void PlayReveal(SpinOutcome outcome, ZoneType zoneType, Action onComplete)
        {
            // Fire-and-forget: the burst plays alongside the highlight tween, not gating onComplete, since
            // nothing downstream needs to wait on a purely cosmetic flourish.
            if (!outcome.IsBomb && (zoneType != ZoneType.Normal || outcome.UnitValue >= BigRewardUnitValue))
                _vfx.PlayRewardBurst();

            _wheel.HighlightSlot(outcome.SlotIndex, onComplete);
        }

        public void PlayRewardGranted(SpinOutcome outcome, Action onComplete) =>
            _bank.FlyIn(outcome, _wheel.SlotWorldPosition(outcome.SlotIndex), onComplete);

        public void PlayBomb(Action onComplete)
        {
            _vfx.PlayBombImpact();
            _bank.Refresh();
            DOVirtual.DelayedCall(0.4f, () => onComplete());
        }

        public void ShowGameOver(int zoneReached, bool continueOffered, int continueCost) =>
            _popups.ShowGameOver(zoneReached, continueOffered, continueCost);

        public void HideGameOver() => _popups.HideGameOver();

        public void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared) =>
            _popups.ShowCashOut(haul, zonesCleared);

        public void HideCashOut() => _popups.HideCashOut();

        public void ShowGiveUpConfirm(int rewardsAtStake) => _popups.ShowGiveUpConfirm(rewardsAtStake);

        public void HideGiveUpConfirm() => _popups.HideGiveUpConfirm();

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
