using System.Collections.Generic;
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

        [Tooltip("PNG cropped to its visible content — icon boxes fit and centre the sprite's full rect.")]
        [SerializeField] private Sprite _icon;

        [SerializeField] private RewardCategory _category = RewardCategory.Points;

        [Tooltip("Amount granted at zone 1, before zone scaling.")]
        [Min(1)]
        [SerializeField] private int _defaultBaseAmount = 1;

        public string Id
        {
            get { return string.IsNullOrEmpty(_id) ? name : _id; }
        }
        public RewardId RewardId
        {
            get { return new RewardId(Id); }
        }
        public Sprite Icon
        {
            get { return _icon; }
        }
        public RewardCategory Category
        {
            get { return _category; }
        }
        public int DefaultBaseAmount
        {
            get { return _defaultBaseAmount; }
        }

        /// <summary>The shard ceiling from the design brief: Points rewards never exceed this.</summary>
        public const int POINTS_CEILING = 5;

        /// <summary>
        /// Stackability and per-drop ceiling, one row per category. A new category must add a row here,
        /// so no reward can be quietly authored as stackable.
        /// </summary>
        private static readonly Dictionary<RewardCategory, (bool Stackable, int MaxAmountPerDrop)> CATEGORY_RULES =
            new Dictionary<RewardCategory, (bool Stackable, int MaxAmountPerDrop)>
            {
                { RewardCategory.Points, (true, POINTS_CEILING) },
                { RewardCategory.Weapon, (false, 0) },
                { RewardCategory.Consumable, (true, 0) },
                { RewardCategory.Cosmetic, (false, 0) },
                { RewardCategory.Currency, (true, 0) },
                { RewardCategory.Chest, (false, 0) },
            };

        /// <summary>Whether more than one of this reward can be granted at once. See <see cref="CATEGORY_RULES"/>.</summary>
        public bool IsStackable
        {
            get { return CATEGORY_RULES[_category].Stackable; }
        }

        /// <summary>Hard ceiling on a single drop's count after zone scaling, or 0 for no ceiling. See <see cref="CATEGORY_RULES"/>.</summary>
        public int MaxAmountPerDrop
        {
            get { return CATEGORY_RULES[_category].MaxAmountPerDrop; }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id)) _id = name;

            if (!_icon)
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
            else if (MaxAmountPerDrop > 0 && _defaultBaseAmount > MaxAmountPerDrop)
            {
                Debug.LogWarning(
                    $"[Vertigo] Reward '{name}' is {_category}, capped at {MaxAmountPerDrop} per drop, but its " +
                    $"base amount is {_defaultBaseAmount}; clamping it to {MaxAmountPerDrop}.", this);
                _defaultBaseAmount = MaxAmountPerDrop;
            }
        }
#endif
    }
}
