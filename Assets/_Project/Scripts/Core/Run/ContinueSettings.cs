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
        public readonly int MaxContinuesPerRun;

        public ContinueSettings(int baseCost, int costPerZone, int maxContinuesPerRun)
        {
            if (baseCost < 0)
                throw new ArgumentOutOfRangeException(nameof(baseCost), baseCost, "Base cost cannot be negative.");
            if (costPerZone < 0)
                throw new ArgumentOutOfRangeException(nameof(costPerZone), costPerZone, "Per-zone cost cannot be negative.");
            if (maxContinuesPerRun < 0)
                throw new ArgumentOutOfRangeException(nameof(maxContinuesPerRun), maxContinuesPerRun, "Continue count cannot be negative.");

            BaseCost = baseCost;
            CostPerZone = costPerZone;
            MaxContinuesPerRun = maxContinuesPerRun;
        }

        public static ContinueSettings Default => new ContinueSettings(50, 10, 1);
    }
}
