using System;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// One authored slot on a wheel.
    /// <para>
    /// Deliberately a serializable class rather than its own asset. A slice is never shared or referenced
    /// by identity, so an asset per slice would mean dozens of near-empty files that are painful to diff
    /// and reorder. Nested like this, the wheel shows up in the inspector as an eight-row reorderable
    /// list — which is exactly what "content of slices should be changeable from the editor" asks for.
    /// </para>
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

        public SliceKind Kind => _kind;
        public RewardDefinition Reward => _reward;
        public int Weight => _weight;

        public bool IsBomb => _kind == SliceKind.Bomb;

        public int ResolveBaseAmount()
        {
            // A unique drop is always a single item: neither an authored override nor zone scaling can
            // turn a built weapon, a cosmetic or a chest into a stack of five.
            if (_reward != null && !_reward.IsStackable) return 1;

            return _baseAmountOverride > 0 ? _baseAmountOverride
                : _reward != null ? _reward.DefaultBaseAmount
                : 1;
        }

        public SliceBlueprint ToBlueprint()
        {
            if (IsBomb) return SliceBlueprint.CreateBomb(_weight);

            return SliceBlueprint.CreateReward(
                _reward.RewardId,
                ResolveBaseAmount(),
                _weight,
                _reward.EstimatedValue,
                scalable: _reward.IsStackable);
        }
    }
}
