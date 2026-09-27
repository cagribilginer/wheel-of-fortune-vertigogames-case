using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The cash-out chest thresholds: the highest tier whose <see cref="ChestTier.MinTotalValue"/> is at or
    /// below the haul's total value wins. Replaces a hardcoded tuple array of value/reward-id pairs with
    /// authored data, referencing the reward asset directly instead of a magic string id.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Chest Tiers", fileName = "ChestTiers_")]
    public sealed class ChestTierConfig : ScriptableObject
    {
        [SerializeField] private List<ChestTier> _tiers = new List<ChestTier>();

        public RewardDefinition ChestFor(long totalValue)
        {
            RewardDefinition chosen = _tiers.Count > 0 ? _tiers[0].Chest : null;

            for (int i = 0; i < _tiers.Count; i++)
                if (totalValue >= _tiers[i].MinTotalValue) chosen = _tiers[i].Chest;

            return chosen;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _tiers.Sort((a, b) => a.MinTotalValue.CompareTo(b.MinTotalValue));

            for (int i = 0; i < _tiers.Count; i++)
                if (_tiers[i].Chest == null)
                    Debug.LogWarning($"[Vertigo] '{name}': tier {i} has no chest reward assigned.", this);
        }
#endif
    }

    [Serializable]
    public sealed class ChestTier
    {
        [Min(0)] [SerializeField] private long _minTotalValue;
        [SerializeField] private RewardDefinition _chest;

        public long MinTotalValue
        {
            get { return _minTotalValue; }
        }

        public RewardDefinition Chest
        {
            get { return _chest; }
        }
    }
}
