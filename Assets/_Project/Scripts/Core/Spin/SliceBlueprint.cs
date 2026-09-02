using System;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// One slice as <em>authored</em>: the base amount before zone scaling is applied.
    /// <para>
    /// The distinction from <see cref="WheelSlice"/> matters. A blueprint is what a designer typed into the
    /// inspector and is the same at every zone; a <see cref="WheelSlice"/> is what this particular zone is
    /// offering, with the amount already scaled. Keeping them separate is what lets the factory be a pure
    /// function of (blueprint, zone) instead of something that mutates authored data.
    /// </para>
    /// </summary>
    public readonly struct SliceBlueprint
    {
        public readonly SliceKind Kind;
        public readonly RewardId Reward;
        public readonly int BaseAmount;
        public readonly int Weight;
        public readonly int UnitValue;

        /// <summary>
        /// Whether zone scaling applies to this slice's amount. False for unique drops (fully-built weapons,
        /// cosmetics, chests), which are always granted as a single item regardless of zone depth.
        /// </summary>
        public readonly bool Scalable;

        /// <summary>
        /// Hard ceiling on the materialised amount, or 0 for no ceiling. Craft shards (the "Points"
        /// rewards) cap here so a deep zone never turns a small shard drop into a pile of them.
        /// </summary>
        public readonly int MaxAmount;

        private SliceBlueprint(
            SliceKind kind, RewardId reward, int baseAmount, int weight, int unitValue, bool scalable, int maxAmount)
        {
            Kind = kind;
            Reward = reward;
            BaseAmount = baseAmount;
            Weight = weight;
            UnitValue = unitValue;
            Scalable = scalable;
            MaxAmount = maxAmount;
        }

        public static SliceBlueprint CreateReward(
            RewardId reward, int baseAmount, int weight = 1, int unitValue = 1, bool scalable = true, int maxAmount = 0)
        {
            if (reward.IsEmpty)
                throw new ArgumentException("A reward slice must carry a non-empty RewardId.", nameof(reward));
            if (baseAmount < 1)
                throw new ArgumentOutOfRangeException(nameof(baseAmount), baseAmount, "Base amount must be >= 1.");
            if (weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "Weight cannot be negative.");
            if (unitValue < 0)
                throw new ArgumentOutOfRangeException(nameof(unitValue), unitValue, "Unit value cannot be negative.");
            if (maxAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAmount), maxAmount, "Max amount cannot be negative.");
            if (maxAmount > 0 && maxAmount < baseAmount)
                throw new ArgumentOutOfRangeException(
                    nameof(maxAmount), maxAmount, "A ceiling below the base amount would never let zone 1 pay out.");

            return new SliceBlueprint(SliceKind.Reward, reward, baseAmount, weight, unitValue, scalable, maxAmount);
        }

        public static SliceBlueprint CreateBomb(int weight = 1)
        {
            if (weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "Weight cannot be negative.");

            return new SliceBlueprint(SliceKind.Bomb, RewardId.None, 0, weight, 0, scalable: false, maxAmount: 0);
        }

        public bool IsBomb => Kind == SliceKind.Bomb;

        /// <summary>Materialises this blueprint for a specific zone.</summary>
        public WheelSlice ToSlice(int zone, IRewardScaling scaling)
        {
            if (scaling == null) throw new ArgumentNullException(nameof(scaling));

            if (IsBomb) return WheelSlice.CreateBomb(Weight);

            int amount = Scalable ? scaling.Scale(BaseAmount, zone) : BaseAmount;
            if (MaxAmount > 0 && amount > MaxAmount) amount = MaxAmount;
            return WheelSlice.CreateReward(Reward, amount, Weight, UnitValue);
        }
    }
}
