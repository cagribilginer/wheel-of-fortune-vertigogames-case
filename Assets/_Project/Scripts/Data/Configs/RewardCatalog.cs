using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The only bridge from a core-layer <see cref="RewardId"/> back to a sprite.
    /// <para>
    /// The logic never sees a Sprite and the views never invent one; everything goes through here, which
    /// is what keeps the rules testable without an asset database.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Reward Catalog", fileName = "RewardCatalog")]
    public sealed class RewardCatalog : ScriptableObject
    {
        [SerializeField] private List<RewardDefinition> _all = new List<RewardDefinition>();

        // Referenced, not named: the wallet, cash-out and revive pricing key off these two rewards' ids, and
        // an asset reference survives a rename or an Id edit that a hard-coded id string would silently miss.
        [Tooltip("Cash-out converts this reward into the persistent gold balance; gold revives are paid in it.")]
        [SerializeField] private RewardDefinition _goldCurrency;
        [Tooltip("Cash-out converts this reward into the persistent cash balance.")]
        [SerializeField] private RewardDefinition _cashCurrency;

        private Dictionary<string, RewardDefinition> _byId;

        public IReadOnlyList<RewardDefinition> All
        {
            get { return _all; }
        }

        public RewardId GoldCurrency
        {
            get { return CurrencyId(_goldCurrency, "gold"); }
        }
        public RewardId CashCurrency
        {
            get { return CurrencyId(_cashCurrency, "cash"); }
        }

        private RewardId CurrencyId(RewardDefinition currency, string role)
        {
            if (currency == null)
                throw new InvalidOperationException($"Catalog '{name}' has no {role} currency assigned.");

            return currency.RewardId;
        }

        public RewardDefinition Find(RewardId id)
        {
            return Find(id.Value);
        }

        public RewardDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureIndex();
            return _byId.TryGetValue(id, out RewardDefinition definition) ? definition : null;
        }

        // Not "?.": that null-conditional skips Unity's overloaded null check, so a destroyed-but-not-
        // collected ScriptableObject would read as non-null here and fail on the property access instead.
        public Sprite IconFor(RewardId id)
        {
            RewardDefinition definition = Find(id);
            return definition != null ? definition.Icon : null;
        }

        private void EnsureIndex()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, RewardDefinition>(_all.Count);
            for (int i = 0; i < _all.Count; i++)
            {
                RewardDefinition definition = _all[i];
                if (definition == null) continue;

                _byId[definition.Id] = definition;
            }
        }

        // Domain reload and asset edits both invalidate the cache; rebuilding lazily is cheaper than
        // keeping it correct eagerly.
        private void OnEnable()
        {
            _byId = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _byId = null;

            var seen = new HashSet<string>();
            for (int i = 0; i < _all.Count; i++)
            {
                RewardDefinition definition = _all[i];
                if (definition == null)
                {
                    Debug.LogError($"[Vertigo] Catalog '{name}' entry {i} is empty.", this);
                    continue;
                }

                if (!seen.Add(definition.Id))
                    Debug.LogError(
                        $"[Vertigo] Catalog '{name}' has two rewards with id '{definition.Id}'. " +
                        "Ids must be unique or icon lookup becomes ambiguous.", this);
            }

            ValidateCurrency(_goldCurrency, "gold");
            ValidateCurrency(_cashCurrency, "cash");
        }

        private void ValidateCurrency(RewardDefinition currency, string role)
        {
            if (currency == null)
                Debug.LogError($"[Vertigo] Catalog '{name}' has no {role} currency assigned.", this);
            else if (currency.Category != RewardCategory.Currency)
                Debug.LogError(
                    $"[Vertigo] Catalog '{name}' uses '{currency.Id}' as its {role} currency, but it is a " +
                    $"{currency.Category}, not a Currency.", this);
            else if (!_all.Contains(currency))
                Debug.LogError(
                    $"[Vertigo] Catalog '{name}' uses '{currency.Id}' as its {role} currency, but it is not " +
                    "in the catalog, so it has no icon lookup.", this);
        }
#endif
    }
}
