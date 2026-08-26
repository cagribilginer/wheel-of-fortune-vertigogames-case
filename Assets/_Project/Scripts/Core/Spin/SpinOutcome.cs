using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// The decided result of a spin.
    /// <para>
    /// This is produced <em>before</em> any rotation is animated. The tween is then told which slot to stop
    /// on, rather than the result being read back from wherever the wheel happened to land. Deriving the
    /// outcome from a float rotation is how you get off-by-one landings and unreproducible failures.
    /// </para>
    /// </summary>
    public readonly struct SpinOutcome
    {
        public readonly int SlotIndex;
        public readonly SliceKind Kind;
        public readonly RewardId Reward;
        public readonly int Amount;
        public readonly int UnitValue;

        public SpinOutcome(int slotIndex, SliceKind kind, RewardId reward, int amount, int unitValue = 1)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            Reward = reward;
            Amount = amount;
            UnitValue = unitValue;
        }

        public static SpinOutcome FromSlice(int slotIndex, WheelSlice slice) =>
            new SpinOutcome(slotIndex, slice.Kind, slice.Reward, slice.Amount, slice.UnitValue);

        public bool IsBomb => Kind == SliceKind.Bomb;

        public override string ToString() =>
            IsBomb ? $"Slot {SlotIndex}: BOMB" : $"Slot {SlotIndex}: {Reward} x{Amount}";
    }
}
