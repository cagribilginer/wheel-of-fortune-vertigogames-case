using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The rules of progression, and the asset that supplies wheels to Core through IWheelBlueprintProvider.
    /// Band selection and safe/super routing live here, where a designer can see them.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Zone Progression", fileName = "ZoneProgression_")]
    public sealed class ZoneProgressionConfig : ScriptableObject, IWheelBlueprintProvider
    {
        [Header("Intervals")]
        [Min(1)] [SerializeField] private int _safeZoneInterval = 5;
        [Min(1)] [SerializeField] private int _superZoneInterval = 30;

        [Header("Wheels")]
        [SerializeField] private ZoneWheelConfig _defaultNormalWheel;

        [Tooltip("One wheel per special zone type (safe, super). A type listed here ignores the band overrides.")]
        [SerializeField] private List<ZoneTypeWheel> _typeWheels = new List<ZoneTypeWheel>();

        [Tooltip("Sorted ascending on validate. The deepest entry at or below the zone wins.")]
        [SerializeField] private List<ZoneBandOverride> _bandOverrides = new List<ZoneBandOverride>();

        [Header("Economy")]
        [SerializeField] private ScalingStrategySO _scaling;

        public ScalingStrategySO Scaling
        {
            get { return _scaling; }
        }

        public ZoneClassifier CreateClassifier()
        {
            return new ZoneClassifier(_safeZoneInterval, _superZoneInterval);
        }

        public WheelBlueprint GetBlueprint(int zone, ZoneType zoneType)
        {
            ZoneWheelConfig config = ResolveConfig(zone, zoneType);
            return config ? config.ToBlueprint() : null;
        }

        /// <summary>
        /// The authored theme of the wheel that actually backs this zone, band overrides included, so a designer
        /// can restyle a band without the presentation inferring a theme from the tier.
        /// </summary>
        public WheelThemeConfig ThemeFor(int zone, ZoneType zoneType)
        {
            ZoneWheelConfig config = ResolveConfig(zone, zoneType);
            return config ? config.Theme : null;
        }

        private ZoneWheelConfig ResolveConfig(int zone, ZoneType zoneType)
        {
            for (int i = 0; i < _typeWheels.Count; i++)
            {
                if (_typeWheels[i].ZoneType == zoneType) return _typeWheels[i].Wheel;
            }

            ZoneWheelConfig chosen = _defaultNormalWheel;

            for (int i = 0; i < _bandOverrides.Count; i++)
            {
                ZoneBandOverride band = _bandOverrides[i];
                if (band == null || !band.Wheel) continue;
                if (band.FromZone <= zone) chosen = band.Wheel;
            }

            return chosen;
        }

#if UNITY_EDITOR
        // An empty list slot sorts last.
        private static int BandSortKey(ZoneBandOverride band)
        {
            return band != null ? band.FromZone : int.MaxValue;
        }

        private void OnValidate()
        {
            // Sorting here means ResolveConfig can rely on "last match wins" rather than re-sorting per spin.
            _bandOverrides.Sort((a, b) => BandSortKey(a).CompareTo(BandSortKey(b)));

            if (_superZoneInterval % _safeZoneInterval != 0)
                Debug.LogWarning(
                    $"[Vertigo] Super interval ({_superZoneInterval}) is not a multiple of the safe interval " +
                    $"({_safeZoneInterval}). Some super zones will not also be safe zones, which reads as " +
                    "inconsistent progression.", this);

            RequireWheel(_defaultNormalWheel, "default normal", WheelTier.Bronze);
            RequireTypeWheel(ZoneType.Safe, WheelTier.Silver);
            RequireTypeWheel(ZoneType.Super, WheelTier.Golden);

            if (!_scaling)
                Debug.LogError($"[Vertigo] Progression '{name}' has no scaling strategy assigned.", this);
        }

        private void RequireTypeWheel(ZoneType zoneType, WheelTier expectedTier)
        {
            for (int i = 0; i < _typeWheels.Count; i++)
            {
                if (_typeWheels[i].ZoneType != zoneType) continue;

                RequireWheel(_typeWheels[i].Wheel, zoneType.ToString().ToLowerInvariant(), expectedTier);
                return;
            }

            Debug.LogError($"[Vertigo] Progression '{name}' lists no wheel for {zoneType} zones.", this);
        }

        private void RequireWheel(ZoneWheelConfig wheel, string role, WheelTier expectedTier)
        {
            if (!wheel)
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
