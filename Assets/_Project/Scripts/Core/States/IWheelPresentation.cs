using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Everything the flow needs the screen to do, expressed without a single Unity type.
    /// <para>
    /// Each animating call takes a completion callback rather than returning: the state machine advances
    /// when the presentation says it has finished, which lets a test substitute an implementation that
    /// completes instantly and drive a sixty-zone run in microseconds with no scene and no frames.
    /// </para>
    /// </summary>
    public interface IWheelPresentation
    {
        /// <summary>Re-themes the wheel for a new zone and scrolls the zone map to it.</summary>
        void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete);

        /// <summary>Mirrors the current input legality onto the buttons. Never decides it.</summary>
        void SetInputState(bool canSpin, bool canLeave, bool canGiveUp);

        /// <summary>Rotates the wheel to a slot the logic has already committed to.</summary>
        void PlaySpin(int slotIndex, Action onComplete);

        /// <summary>
        /// The landing beat. <paramref name="zoneType"/> is passed alongside the outcome so the screen can
        /// flag a safe/super zone clear the moment the slot lands, not only once the reward reaches the bank.
        /// </summary>
        void PlayReveal(SpinOutcome outcome, ZoneType zoneType, Action onComplete);

        void PlayRewardGranted(SpinOutcome outcome, Action onComplete);

        void PlayBomb(Action onComplete);

        /// <summary>
        /// The bomb defeat / revive screen. <paramref name="lostHaul"/> is what the bomb just took (the run
        /// bank is already empty by now) so the screen can show the player what a revive would win back;
        /// <paramref name="playerGold"/> is the persistent wallet balance shown in the corner. The two
        /// revive offers are independent: paid needs an affordable, unused continue slot, ad only an unused one.
        /// </summary>
        void ShowGameOver(
            int zoneReached, IReadOnlyList<BankEntry> lostHaul, int playerGold,
            bool goldReviveOffered, int goldReviveCost, bool adReviveOffered);
        void HideGameOver();

        /// <summary>
        /// The cash-out summary. Nothing is committed yet — the player can still cancel back to the wheel —
        /// so this shows the live haul for them to weigh.
        /// </summary>
        void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared);

        /// <summary>Plain dismissal (the player cancelled): close the summary, no reward flourish.</summary>
        void HideCashOut();

        /// <summary>
        /// The player confirmed "CLAIM &amp; LEAVE". The wallet has already been credited; this plays the
        /// claim celebration (chest punch, jingle, counter count-up) and calls <paramref name="onComplete"/>
        /// once it has finished, at which point the state machine resets the run.
        /// </summary>
        void ClaimCashOut(Action onComplete);

        void ShowGiveUpConfirm(int rewardsAtStake);
        void HideGiveUpConfirm();
    }
}
