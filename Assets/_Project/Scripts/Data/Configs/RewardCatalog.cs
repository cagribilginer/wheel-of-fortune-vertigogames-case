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

        private Dictionary<string, RewardDefinition> _byId;

        public IReadOnlyList<RewardDefinition> All => _all;

        public RewardDefinition Find(RewardId id) => Find(id.Value);

        public RewardDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureIndex();
            return _byId.TryGetValue(id, out RewardDefinition definition) ? definition : null;
        }

        public Sprite IconFor(RewardId id) => Find(id)?.Icon;

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
        private void OnEnable() => _byId = null;

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
        }
#endif
    }
}
