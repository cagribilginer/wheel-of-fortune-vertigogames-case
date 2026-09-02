using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// One authored reward: its stable id, its sprite, and what it is worth.
    /// <para>
    /// The core layer only ever sees <see cref="RewardId"/>; this asset is the single place a sprite is
    /// attached to one. Adding a reward is a right-click in the Project window and no code at all.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Vertigo/Rewards/Reward Definition", fileName = "Reward_")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [Tooltip("Stable key used by the logic layer. Defaults to the asset filename.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private RewardCategory _category = RewardCategory.Points;

        [Tooltip("Amount granted at zone 1, before zone scaling.")]
        [Min(1)]
        [SerializeField] private int _defaultBaseAmount = 10;

        [Tooltip("Relative worth per unit. Only sizes the cash-out chest; never affects odds.")]
        [Min(0)]
        [SerializeField] private int _estimatedValue = 1;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public RewardId RewardId => new RewardId(Id);
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public RewardCategory Category => _category;
        public int DefaultBaseAmount => _defaultBaseAmount;
        public int EstimatedValue => _estimatedValue;

        /// <summary>
        /// Whether more than one of this reward can be granted at once. Consumables, currencies and craft
        /// shards (the "Points" rewards) stack — their amounts also grow with zone depth. A fully-built
        /// weapon, a cosmetic or a chest is a single unique drop: its count is always 1 and zone scaling
        /// never touches it.
        /// </summary>
        public bool IsStackable =>
            _category == RewardCategory.Consumable ||
            _category == RewardCategory.Currency ||
            _category == RewardCategory.Points;

        /// <summary>
        /// Hard ceiling on a single drop's count after zone scaling, or 0 for no ceiling. Craft shards
        /// (the "Points" rewards) top out at 5 however deep the run goes; consumables and currencies are
        /// left uncapped so a deep run still feels rewarding.
        /// </summary>
        public int MaxAmountPerDrop => _category == RewardCategory.Points ? PointsCeiling : 0;

        /// <summary>The shard ceiling from the design brief: Points rewards never exceed this.</summary>
        public const int PointsCeiling = 5;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id)) _id = name;

            if (_icon == null)
                Debug.LogWarning($"[Vertigo] Reward '{name}' has no icon assigned.", this);

            // A unique drop is a single item by definition; a non-1 base amount here is a mistake and would
            // otherwise show a misleading count in the inspector and on the wheel.
            if (!IsStackable && _defaultBaseAmount != 1)
            {
                Debug.LogWarning(
                    $"[Vertigo] Reward '{name}' is {_category} (not stackable) but its base amount is " +
                    $"{_defaultBaseAmount}; forcing it to 1.", this);
                _defaultBaseAmount = 1;
            }
        }
#endif
    }
}
