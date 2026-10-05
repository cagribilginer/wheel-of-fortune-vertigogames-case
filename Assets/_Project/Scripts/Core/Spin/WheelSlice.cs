using System;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// One of the eight slots on a wheel, with its amount already scaled for the current zone.
    /// Immutable: a slice is a snapshot of what this zone is offering, not a mutable authoring record.
    /// </summary>
    public readonly struct WheelSlice
    {
        public readonly SliceKind Kind;
        public readonly RewardId Reward;
        public readonly int Amount;
        public readonly int Weight;

        private WheelSlice(SliceKind kind, RewardId reward, int amount, int weight)
        {
            Kind = kind;
            Reward = reward;
            Amount = amount;
            Weight = weight;
        }

        public static WheelSlice CreateReward(RewardId reward, int amount, int weight = 1)
        {
            if (reward.IsEmpty)
                throw new ArgumentException("A reward slice must carry a non-empty RewardId.", nameof(reward));
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "A reward slice must grant at least 1.");
            if (weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "Weight cannot be negative.");

            return new WheelSlice(SliceKind.Reward, reward, amount, weight);
        }

        public static WheelSlice CreateBomb(int weight = 1)
        {
            if (weight < 1)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, SliceBlueprint.BOMB_WEIGHT_MESSAGE);

            return new WheelSlice(SliceKind.Bomb, RewardId.None, 0, weight);
        }

        public bool IsBomb
        {
            get { return Kind == SliceKind.Bomb; }
        }
    }
}
