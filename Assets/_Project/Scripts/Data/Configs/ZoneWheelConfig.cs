using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// An authored wheel: eight slices, a tier, and a theme.
    /// <para>
    /// This asset is the answer to "content of slices of each wheel should also be changeable from the
    /// editor". Edit the list, press play, the wheel is different — no recompile.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Wheel/Zone Wheel Config", fileName = "Wheel_")]
    public sealed class ZoneWheelConfig : ScriptableObject
    {
        [SerializeField] private WheelTier _tier = WheelTier.Bronze;
        [SerializeField] private WheelThemeConfig _theme;

        [Tooltip("Exactly eight, matching the eight slots in the wheel artwork.")]
        [SerializeField] private List<WheelSliceEntry> _slices = new List<WheelSliceEntry>();

        [Tooltip("Off by default: a fixed slot order makes this list a literal picture of the wheel.")]
        [SerializeField] private bool _shuffleSliceOrder;

        public WheelTier Tier => _tier;
        public WheelThemeConfig Theme => _theme;
        public IReadOnlyList<WheelSliceEntry> Slices => _slices;
        public bool ShuffleSliceOrder => _shuffleSliceOrder;

        public WheelBlueprint ToBlueprint()
        {
            var blueprints = new List<SliceBlueprint>(_slices.Count);

            for (int i = 0; i < _slices.Count; i++)
            {
                WheelSliceEntry entry = _slices[i];
                if (entry == null) continue;
                if (!entry.IsBomb && entry.Reward == null) continue;

                blueprints.Add(entry.ToBlueprint());
            }

            return new WheelBlueprint(_tier, blueprints, _shuffleSliceOrder);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_slices.Count != WheelModel.StandardSliceCount)
                Debug.LogError(
                    $"[Vertigo] Wheel '{name}' has {_slices.Count} slices; the artwork has " +
                    $"{WheelModel.StandardSliceCount} slots.", this);

            int bombs = 0;
            int totalWeight = 0;
            var seen = new HashSet<string>();

            for (int i = 0; i < _slices.Count; i++)
            {
                WheelSliceEntry entry = _slices[i];
                if (entry == null)
                {
                    Debug.LogError($"[Vertigo] Wheel '{name}' slice {i} is null.", this);
                    continue;
                }

                totalWeight += entry.Weight;

                if (entry.IsBomb) { bombs++; continue; }

                if (entry.Reward == null)
                {
                    Debug.LogError($"[Vertigo] Wheel '{name}' slice {i} is a reward with no RewardDefinition.", this);
                    continue;
                }

                if (!seen.Add(entry.Reward.Id))
                    Debug.LogWarning(
                        $"[Vertigo] Wheel '{name}' lists '{entry.Reward.Id}' on more than one slice. " +
                        "Allowed, but usually unintended.", this);
            }

            // A normal wheel without its bomb is a free run; a safe or super wheel with one breaks the
            // whole promise of the zone. Both are worth failing loudly on.
            if (_tier == WheelTier.Bronze && bombs != 1)
                Debug.LogError($"[Vertigo] Bronze wheel '{name}' must carry exactly one bomb, found {bombs}.", this);

            if (_tier != WheelTier.Bronze && bombs != 0)
                Debug.LogError(
                    $"[Vertigo] {_tier} wheel '{name}' is risk-free and must carry no bomb, found {bombs}.", this);

            if (totalWeight <= 0)
                Debug.LogError($"[Vertigo] Wheel '{name}' has zero total weight; no slice could ever be drawn.", this);

            if (_theme == null)
                Debug.LogWarning($"[Vertigo] Wheel '{name}' has no theme assigned.", this);
        }
#endif
    }
}
