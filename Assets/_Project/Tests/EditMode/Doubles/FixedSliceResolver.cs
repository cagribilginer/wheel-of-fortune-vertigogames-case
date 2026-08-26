using System.Collections.Generic;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>Always lands on the same slot. Lets a test drive an exact bomb-or-reward outcome.</summary>
    public sealed class FixedSliceResolver : ISliceResolver
    {
        private int _index;

        public FixedSliceResolver(int index) => _index = index;

        public void LandOn(int index) => _index = index;

        public int Resolve(IReadOnlyList<WheelSlice> slices) => _index;
    }
}
