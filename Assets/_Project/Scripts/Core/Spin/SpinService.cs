using System;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// Turns "the player pressed spin" into a decided <see cref="SpinOutcome"/>.
    /// Owns no state and touches no presentation, so a full run can be simulated in a unit test.
    /// </summary>
    public sealed class SpinService
    {
        private readonly ISliceResolver _resolver;

        public SpinService(ISliceResolver resolver) =>
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

        public SpinOutcome Spin(WheelModel wheel)
        {
            if (wheel == null) throw new ArgumentNullException(nameof(wheel));

            int slotIndex = _resolver.Resolve(wheel.Slices);
            return SpinOutcome.FromSlice(slotIndex, wheel[slotIndex]);
        }
    }
}
