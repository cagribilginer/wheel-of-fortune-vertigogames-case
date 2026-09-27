using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Everything the flow needs the screen to do, expressed without a single Unity type. Each animating
    /// call takes a completion callback rather than returning, so the state machine advances only when
    /// the presentation says it has finished — and a test double can complete instantly with no scene.
    /// </summary>
    public interface IWheelPresentation
    {
        /// <summary>Re-themes the wheel for a new zone and scrolls the zone map to it.</summary>
        void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete);

        /// <summary>Mirrors the current input legality onto the buttons. Never decides it.</summary>
        void SetInputState(bool canSpin, bool canLeave);

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
        /// <paramref name="playerGold"/> and <paramref name="playerCash"/> are the persistent wallet
        /// balances shown in the corner — the same two numbers <see cref="ShowCashOut"/> shows, since both
        /// screens display the actual wallet, never a haul total wearing a currency's name. The two revive
        /// offers are independent: paid needs an affordable, unused continue slot, ad only an unused one.
        /// </summary>
        void ShowGameOver(
            int zoneReached, IReadOnlyList<BankEntry> lostHaul, int playerGold, int playerCash,
            bool goldReviveOffered, int goldReviveCost, bool adReviveOffered);
        void HideGameOver();

        /// <summary>
        /// The cash-out summary. Nothing is committed yet — the player can still cancel back to the wheel —
        /// so this shows the live haul for them to weigh, plus the wallet balances (<paramref
        /// name="playerGold"/>, <paramref name="playerCash"/>) as they stand before this claim lands.
        /// </summary>
        void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared, int playerGold, int playerCash);

        /// <summary>Plain dismissal (the player cancelled): close the summary, no reward flourish.</summary>
        void HideCashOut();

        /// <summary>
        /// The player confirmed "CLAIM &amp; LEAVE". The wallet has already been credited;
        /// <paramref name="playerGold"/> and <paramref name="playerCash"/> are the resulting (post-claim)
        /// balances, which this counts the summary's own cash/gold row up to during the celebration (chest
        /// punch, jingle) before calling <paramref name="onComplete"/>, at which point the state machine
        /// resets the run.
        /// </summary>
        void ClaimCashOut(int playerGold, int playerCash, Action onComplete);
    }
}
