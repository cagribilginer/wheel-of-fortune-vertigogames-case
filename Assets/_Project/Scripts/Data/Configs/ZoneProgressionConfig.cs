using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The rules of progression, and the asset that supplies wheels to the core layer.
    /// <para>
    /// Implementing <see cref="IWheelBlueprintProvider"/> is what keeps the factory engine-free: this asset
    /// is the plug, the interface in Core is the port. Band selection and safe/super routing happen here,
    /// where a designer can see them.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Zone Progression", fileName = "ZoneProgression_")]
    public sealed class ZoneProgressionConfig : ScriptableObject, IWheelBlueprintProvider
    {
        [Header("Intervals")]
        [Min(1)] [SerializeField] private int _safeZoneInterval = 5;
        [Min(1)] [SerializeField] private int _superZoneInterval = 30;

        [Header("Wheels")]
        [SerializeField] private ZoneWheelConfig _defaultNormalWheel;
        [SerializeField] private ZoneWheelConfig _safeWheel;
        [SerializeField] private ZoneWheelConfig _superWheel;

        [Tooltip("Sorted ascending on validate. The deepest entry at or below the zone wins.")]
        [SerializeField] private List<ZoneBandOverride> _bandOverrides = new List<ZoneBandOverride>();

        [Header("Economy")]
        [SerializeField] private ScalingStrategySO _scaling;

        [Header("Demo")]
        [Tooltip("0 = endless, which is what ships. Only set this to shorten a recording.")]
        [Min(0)] [SerializeField] private int _demoMaxZone;

        public int SafeZoneInterval => _safeZoneInterval;
        public int SuperZoneInterval => _superZoneInterval;
        public ScalingStrategySO Scaling => _scaling;
        public int DemoMaxZone => _demoMaxZone;

        public ZoneClassifier CreateClassifier() => new ZoneClassifier(_safeZoneInterval, _superZoneInterval);

        public WheelBlueprint GetBlueprint(int zone, ZoneType zoneType)
        {
            ZoneWheelConfig config = ResolveConfig(zone, zoneType);
            return config == null ? null : config.ToBlueprint();
        }

        private ZoneWheelConfig ResolveConfig(int zone, ZoneType zoneType)
        {
            switch (zoneType)
            {
                case ZoneType.Super: return _superWheel;
                case ZoneType.Safe: return _safeWheel;
            }

            ZoneWheelConfig chosen = _defaultNormalWheel;

            for (int i = 0; i < _bandOverrides.Count; i++)
            {
                ZoneBandOverride band = _bandOverrides[i];
                if (band?.Wheel == null) continue;
                if (band.FromZone <= zone) chosen = band.Wheel;
            }

            return chosen;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Sorting here means ResolveConfig can rely on "last match wins" rather than re-sorting per spin.
            _bandOverrides.Sort((a, b) =>
                (a?.FromZone ?? int.MaxValue).CompareTo(b?.FromZone ?? int.MaxValue));

            if (_superZoneInterval % _safeZoneInterval != 0)
                Debug.LogWarning(
                    $"[Vertigo] Super interval ({_superZoneInterval}) is not a multiple of the safe interval " +
                    $"({_safeZoneInterval}). Some super zones will not also be safe zones, which reads as " +
                    "inconsistent progression.", this);

            RequireWheel(_defaultNormalWheel, "default normal", WheelTier.Bronze);
            RequireWheel(_safeWheel, "safe", WheelTier.Silver);
            RequireWheel(_superWheel, "super", WheelTier.Golden);

            if (_scaling == null)
                Debug.LogError($"[Vertigo] Progression '{name}' has no scaling strategy assigned.", this);

            if (_demoMaxZone > 0)
                Debug.LogWarning(
                    $"[Vertigo] Progression '{name}' caps the run at zone {_demoMaxZone}. " +
                    "This is a recording aid and must be 0 in the shipped build.", this);
        }

        private void RequireWheel(ZoneWheelConfig wheel, string role, WheelTier expectedTier)
        {
            if (wheel == null)
            {
                Debug.LogError($"[Vertigo] Progression '{name}' has no {role} wheel assigned.", this);
                return;
            }

            if (wheel.Tier != expectedTier)
                Debug.LogWarning(
                    $"[Vertigo] The {role} wheel '{wheel.name}' is tier {wheel.Tier}, expected {expectedTier}.", this);
        }
#endif
    }
}
