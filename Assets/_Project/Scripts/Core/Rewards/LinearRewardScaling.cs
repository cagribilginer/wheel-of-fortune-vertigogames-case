using System;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// <c>ceil(baseAmount * (1 + growthPerZone * (zone - 1)))</c>.
    /// Zone 1 pays the authored amount exactly; with the default 0.25 growth, zone 10 pays 3.25x and
    /// zone 29 pays 8x. Monotonic, never zero, and readable to a designer at a glance.
    /// </summary>
    public sealed class LinearRewardScaling : IRewardScaling
    {
        public const double DefaultGrowthPerZone = 0.25d;

        private readonly double _growthPerZone;

        public LinearRewardScaling(double growthPerZone = DefaultGrowthPerZone)
        {
            if (growthPerZone < 0d)
                throw new ArgumentOutOfRangeException(nameof(growthPerZone), growthPerZone, "Growth cannot be negative.");

            _growthPerZone = growthPerZone;
        }

        public int Scale(int baseAmount, int zone) =>
            RewardScalingMath.Apply(baseAmount, zone, 1d + _growthPerZone * (zone - 1));
    }
}
