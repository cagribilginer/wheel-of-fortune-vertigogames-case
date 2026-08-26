using System;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// Multiplies by a fixed factor once per completed block of zones, so amounts jump at readable
    /// milestones instead of creeping. The second real implementation that earns <see cref="IRewardScaling"/>
    /// its keep as a Strategy rather than an interface tax.
    /// </summary>
    public sealed class StepRewardScaling : IRewardScaling
    {
        private readonly int _zonesPerStep;
        private readonly double _multiplierPerStep;

        public StepRewardScaling(int zonesPerStep = 5, double multiplierPerStep = 1.6d)
        {
            if (zonesPerStep < 1)
                throw new ArgumentOutOfRangeException(nameof(zonesPerStep), zonesPerStep, "Zones per step must be >= 1.");
            if (multiplierPerStep < 1d)
                throw new ArgumentOutOfRangeException(nameof(multiplierPerStep), multiplierPerStep, "Multiplier must be >= 1.");

            _zonesPerStep = zonesPerStep;
            _multiplierPerStep = multiplierPerStep;
        }

        public int Scale(int baseAmount, int zone)
        {
            RewardScalingMath.ValidateZone(zone);

            int steps = (zone - 1) / _zonesPerStep;
            return RewardScalingMath.Apply(baseAmount, zone, Math.Pow(_multiplierPerStep, steps));
        }
    }
}
