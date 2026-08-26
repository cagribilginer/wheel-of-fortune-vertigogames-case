using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>Steady growth per zone. The shipped default.</summary>
    [CreateAssetMenu(menuName = "Vertigo/Scaling/Linear", fileName = "Scaling_Linear")]
    public sealed class LinearScalingSO : ScalingStrategySO
    {
        [Tooltip("0.25 means zone 10 pays 3.25x the authored amount and zone 29 pays 8x.")]
        [Min(0f)]
        [SerializeField] private float _growthPerZone = 0.25f;

        private LinearRewardScaling _cached;
        private float _cachedGrowth = -1f;

        public override int Scale(int baseAmount, int zone)
        {
            // Rebuilt only when a designer edits the value, so play mode allocates nothing.
            if (_cached == null || !Mathf.Approximately(_cachedGrowth, _growthPerZone))
            {
                _cached = new LinearRewardScaling(_growthPerZone);
                _cachedGrowth = _growthPerZone;
            }

            return _cached.Scale(baseAmount, zone);
        }

        private void OnDisable() => _cached = null;
    }
}
