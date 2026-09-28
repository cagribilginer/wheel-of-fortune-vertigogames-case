using System;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// One authored slot on a wheel: a serializable class rather than its own asset, so a wheel reads as an
    /// eight-row reorderable list in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class WheelSliceEntry
    {
        [SerializeField] private SliceKind _kind = SliceKind.Reward;
        [SerializeField] private RewardDefinition _reward;

        [Tooltip("Leave at 0 to use the reward's own default base amount.")]
        [Min(0)]
        [SerializeField] private int _baseAmountOverride;

        [Tooltip("Relative chance. All slices ship at 1, which makes the bomb an honest 1 in 8.")]
        [Min(0)]
        [SerializeField] private int _weight = 1;

        public SliceKind Kind
        {
            get { return _kind; }
        }
        public RewardDefinition Reward
        {
            get { return _reward; }
        }
        public int Weight
        {
            get { return _weight; }
        }

        public bool IsBomb
        {
            get { return _kind == SliceKind.Bomb; }
        }

        public int ResolveBaseAmount()
        {
            // A unique drop is always a single item: neither an authored override nor zone scaling can
            // turn a built weapon, a cosmetic or a chest into a stack of five.
            if (_reward && !_reward.IsStackable) return 1;

            int amount = _baseAmountOverride > 0 ? _baseAmountOverride
                : _reward ? _reward.DefaultBaseAmount
                : 1;

            // Nor can it start above its category's per-drop ceiling (a Points shard caps at 5).
            int ceiling = _reward ? _reward.MaxAmountPerDrop : 0;
            return ceiling > 0 && amount > ceiling ? ceiling : amount;
        }

        public SliceBlueprint ToBlueprint()
        {
            if (IsBomb) return SliceBlueprint.CreateBomb(_weight);

            return SliceBlueprint.CreateReward(
                _reward.RewardId,
                ResolveBaseAmount(),
                _weight,
                scalable: _reward.IsStackable,
                maxAmount: _reward.MaxAmountPerDrop);
        }
    }
}
