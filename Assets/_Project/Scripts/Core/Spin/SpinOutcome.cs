using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// The decided result of a spin, produced before any rotation is animated: the tween is told which slot
    /// to stop on instead of reading the result back from a float angle.
    /// </summary>
    public readonly struct SpinOutcome
    {
        public readonly int SlotIndex;
        public readonly SliceKind Kind;
        public readonly RewardId Reward;
        public readonly int Amount;

        public SpinOutcome(int slotIndex, SliceKind kind, RewardId reward, int amount)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            Reward = reward;
            Amount = amount;
        }

        public static SpinOutcome FromSlice(int slotIndex, WheelSlice slice)
        {
            return new SpinOutcome(slotIndex, slice.Kind, slice.Reward, slice.Amount);
        }

        public bool IsBomb
        {
            get { return Kind == SliceKind.Bomb; }
        }

        public override string ToString()
        {
            return IsBomb ? $"Slot {SlotIndex}: BOMB" : $"Slot {SlotIndex}: {Reward} x{Amount}";
        }
    }
}
