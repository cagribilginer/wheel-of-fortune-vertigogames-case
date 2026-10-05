using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>
    /// Weighted-random selection that never lands on a bomb. Lets a long-run or overflow test survive the
    /// normal zones without authoring a zero-weight bomb, which the wheel factory rightly refuses.
    /// </summary>
    public sealed class RewardOnlyResolver : ISliceResolver
    {
        private readonly IRandomProvider _random;

        public RewardOnlyResolver(IRandomProvider random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Resolve(IReadOnlyList<WheelSlice> slices)
        {
            int total = 0;
            for (int i = 0; i < slices.Count; i++)
                if (!slices[i].IsBomb) total += slices[i].Weight;

            int roll = _random.Next(total);
            for (int i = 0; i < slices.Count; i++)
            {
                if (slices[i].IsBomb) continue;
                roll -= slices[i].Weight;
                if (roll < 0) return i;
            }

            throw new InvalidOperationException("The wheel has no reward slice to land on.");
        }
    }
}
