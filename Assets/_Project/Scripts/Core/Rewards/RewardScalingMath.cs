using System;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// Shared guards for reward scaling: argument validation, rounding, and overflow clamping.
    /// Extracted so every strategy applies the same rules rather than each re-deriving them.
    /// </summary>
    internal static class RewardScalingMath
    {
        internal static void ValidateZone(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone), zone, "Zones are 1-indexed; the first zone is 1.");
        }

        internal static int Apply(int baseAmount, int zone, double multiplier)
        {
            ValidateZone(zone);

            if (baseAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(baseAmount), baseAmount, "Base amount must be >= 1.");

            double scaled = Math.Ceiling(baseAmount * multiplier);

            // Deep zones are reachable in an endless run, so clamp rather than let the cast wrap negative.
            if (scaled >= int.MaxValue) return int.MaxValue;

            return scaled < 1d ? 1 : (int)scaled;
        }
    }
}
