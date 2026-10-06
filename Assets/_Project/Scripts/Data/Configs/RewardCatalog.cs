using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// The only bridge from a core-layer <see cref="RewardId"/> back to a sprite, so the rules never need an
    /// asset database.
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Config/Reward Catalog", fileName = "RewardCatalog")]
    public sealed class RewardCatalog : ScriptableObject
    {
        [SerializeField] private List<RewardDefinition> _all = new List<RewardDefinition>();

        // An asset reference, not an id string, so a rename cannot silently break revive pricing. Other wallet
        // currencies need no entry here: cash-out banks all of them (see CurrencyIds).
        [Tooltip("The currency gold revives are paid in.")]
        [SerializeField] private RewardDefinition _goldCurrency;

        private Dictionary<string, RewardDefinition> _byId;
        private List<RewardId> _currencyIds;

        #region Lookup
        public IReadOnlyList<RewardDefinition> All
        {
            get { return _all; }
        }

        public RewardId GoldCurrency
        {
            get { return CurrencyId(_goldCurrency, "gold"); }
        }

        /// <summary>Every reward whose category is a wallet currency: what a cash-out banks into the wallet.</summary>
        public IReadOnlyCollection<RewardId> CurrencyIds
        {
            get
            {
                if (_currencyIds != null) return _currencyIds;

                _currencyIds = new List<RewardId>();
                for (int i = 0; i < _all.Count; i++)
                {
                    if (_all[i] && _all[i].Category.IsWalletCurrency) _currencyIds.Add(_all[i].RewardId);
                }
                return _currencyIds;
            }
        }

        private RewardId CurrencyId(RewardDefinition currency, string role)
        {
            if (!currency)
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
            return definition ? definition.Icon : null;
        }

        private void EnsureIndex()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, RewardDefinition>(_all.Count);
            for (int i = 0; i < _all.Count; i++)
            {
                RewardDefinition definition = _all[i];
                if (!definition) continue;

                _byId[definition.Id] = definition;
            }
        }

        // Domain reload and asset edits both invalidate the cache; rebuilding lazily is cheaper than
        // keeping it correct eagerly.
        private void OnEnable()
        {
            _byId = null;
            _currencyIds = null;
        }
        #endregion

        #region Validation
#if UNITY_EDITOR
        private void OnValidate()
        {
            _byId = null;
            _currencyIds = null;

            var seen = new HashSet<string>();
            for (int i = 0; i < _all.Count; i++)
            {
                RewardDefinition definition = _all[i];
                if (!definition)
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
        }

        private void ValidateCurrency(RewardDefinition currency, string role)
        {
            if (!currency)
                Debug.LogError($"[Vertigo] Catalog '{name}' has no {role} currency assigned.", this);
            else if (!currency.Category.IsWalletCurrency)
                Debug.LogError(
                    $"[Vertigo] Catalog '{name}' uses '{currency.Id}' as its {role} currency, but it is a " +
                    $"{currency.Category.name}, which is not a wallet currency category.", this);
            else if (!_all.Contains(currency))
                Debug.LogError(
                    $"[Vertigo] Catalog '{name}' uses '{currency.Id}' as its {role} currency, but it is not " +
                    "in the catalog, so it has no icon lookup.", this);
        }
#endif
        #endregion
    }
}
