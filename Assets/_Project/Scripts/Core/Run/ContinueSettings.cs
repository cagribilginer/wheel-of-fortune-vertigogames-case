using System;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// Pure mirror of the ContinueConfig asset, so the continue rules stay testable without Unity.
    /// </summary>
    public readonly struct ContinueSettings
    {
        public readonly int BaseCost;
        public readonly int CostPerZone;

        /// <summary>
        /// Cap on the free ad revive only. The paid gold revive has no per-run cap — it is gated purely by
        /// whether the player can afford its (doubling) price.
        /// </summary>
        public readonly int MaxAdRevivesPerRun;

        public ContinueSettings(int baseCost, int costPerZone, int maxAdRevivesPerRun)
        {
            if (baseCost < 0)
                throw new ArgumentOutOfRangeException(nameof(baseCost), baseCost, "Base cost cannot be negative.");
            if (costPerZone < 0)
                throw new ArgumentOutOfRangeException(nameof(costPerZone), costPerZone, "Per-zone cost cannot be negative.");
            if (maxAdRevivesPerRun < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAdRevivesPerRun), maxAdRevivesPerRun, "Ad revive count cannot be negative.");

            BaseCost = baseCost;
            CostPerZone = costPerZone;
            MaxAdRevivesPerRun = maxAdRevivesPerRun;
        }

        public static ContinueSettings Default => new ContinueSettings(50, 10, 1);
    }
}
