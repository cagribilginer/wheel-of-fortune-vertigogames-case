using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Everything the bomb defeat / revive screen needs to show. Bundled rather than passed as seven loose
    /// parameters to <see cref="IWheelPresentation.ShowGameOver"/> — three ints and two bools sitting next
    /// to each other invite a transposed-argument bug at the call site that the compiler can't catch.
    /// </summary>
    public readonly struct GameOverSummary
    {
        public readonly int ZoneReached;

        /// <summary>What the bomb just took. The run bank is already empty by the time this shows.</summary>
        public readonly IReadOnlyList<BankEntry> LostHaul;

        /// <summary>
        /// The persistent wallet balances shown in the corner — the same two numbers
        /// <see cref="IWheelPresentation.ShowCashOut"/> shows, never a haul total wearing a currency's name.
        /// </summary>
        public readonly int PlayerGold;
        public readonly int PlayerCash;

        /// <summary>The two revive offers are independent: paid needs an affordable, unused continue slot.</summary>
        public readonly bool GoldReviveOffered;
        public readonly int GoldReviveCost;

        /// <summary>Ad revive only needs an unused slot — no wallet cost.</summary>
        public readonly bool AdReviveOffered;

        public GameOverSummary(
            int zoneReached, IReadOnlyList<BankEntry> lostHaul, int playerGold, int playerCash,
            bool goldReviveOffered, int goldReviveCost, bool adReviveOffered)
        {
            ZoneReached = zoneReached;
            LostHaul = lostHaul;
            PlayerGold = playerGold;
            PlayerCash = playerCash;
            GoldReviveOffered = goldReviveOffered;
            GoldReviveCost = goldReviveCost;
            AdReviveOffered = adReviveOffered;
        }
    }
}
