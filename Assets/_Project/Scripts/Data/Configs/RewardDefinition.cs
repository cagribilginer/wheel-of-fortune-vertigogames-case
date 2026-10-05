using System;
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

        [Tooltip("Colour of this reward's number where it is shown as a wallet balance.")]
        [SerializeField] private Color _balanceColor = Color.white;

        [Tooltip("What kind of reward this is: whether it stacks, its per-drop ceiling and whether it is a wallet currency.")]
        [SerializeField] private RewardCategoryDefinition _category;

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
        public Color BalanceColor
        {
            get { return _balanceColor; }
        }
        public RewardCategoryDefinition Category
        {
            get
            {
                if (!_category)
                    throw new InvalidOperationException($"Reward '{name}' has no category assigned.");

                return _category;
            }
        }
        public int DefaultBaseAmount
        {
            get { return _defaultBaseAmount; }
        }

        /// <summary>Whether more than one of this reward can be granted at once. Decided by its category asset.</summary>
        public bool IsStackable
        {
            get { return Category.Stackable; }
        }

        /// <summary>Hard ceiling on a single drop's count after zone scaling, or 0 for no ceiling. Decided by its category asset.</summary>
        public int MaxAmountPerDrop
        {
            get { return Category.MaxAmountPerDrop; }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id)) _id = name;

            if (!_icon)
                Debug.LogWarning($"[Vertigo] Reward '{name}' has no icon assigned.", this);

            if (!_category)
            {
                Debug.LogError($"[Vertigo] Reward '{name}' has no category assigned.", this);
                return;
            }

            // A unique drop is a single item by definition; a non-1 base amount here is a mistake and would
            // otherwise show a misleading count in the inspector and on the wheel.
            if (!IsStackable && _defaultBaseAmount != 1)
            {
                Debug.LogWarning(
                    $"[Vertigo] Reward '{name}' is {_category.name} (not stackable) but its base amount is " +
                    $"{_defaultBaseAmount}; forcing it to 1.", this);
                _defaultBaseAmount = 1;
            }
            else if (MaxAmountPerDrop > 0 && _defaultBaseAmount > MaxAmountPerDrop)
            {
                Debug.LogWarning(
                    $"[Vertigo] Reward '{name}' is {_category.name}, capped at {MaxAmountPerDrop} per drop, but its " +
                    $"base amount is {_defaultBaseAmount}; clamping it to {MaxAmountPerDrop}.", this);
                _defaultBaseAmount = MaxAmountPerDrop;
            }
        }
#endif
    }
}
