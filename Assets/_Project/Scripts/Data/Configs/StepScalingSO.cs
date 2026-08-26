using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>Amounts jump at readable milestones instead of creeping upward every zone.</summary>
    [CreateAssetMenu(menuName = "Vertigo/Scaling/Step", fileName = "Scaling_Step")]
    public sealed class StepScalingSO : ScalingStrategySO
    {
        [Min(1)] [SerializeField] private int _zonesPerStep = 5;
        [Min(1f)] [SerializeField] private float _multiplierPerStep = 1.6f;

        private StepRewardScaling _cached;
        private int _cachedZones = -1;
        private float _cachedMultiplier = -1f;

        public override int Scale(int baseAmount, int zone)
        {
            if (_cached == null || _cachedZones != _zonesPerStep ||
                !Mathf.Approximately(_cachedMultiplier, _multiplierPerStep))
            {
                _cached = new StepRewardScaling(_zonesPerStep, _multiplierPerStep);
                _cachedZones = _zonesPerStep;
                _cachedMultiplier = _multiplierPerStep;
            }

            return _cached.Scale(baseAmount, zone);
        }

        private void OnDisable() => _cached = null;
    }
}
