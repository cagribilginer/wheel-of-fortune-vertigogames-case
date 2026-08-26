using System.Collections.Generic;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>Picks which slot index a spin lands on.</summary>
    public interface ISliceResolver
    {
        int Resolve(IReadOnlyList<WheelSlice> slices);
    }
}
