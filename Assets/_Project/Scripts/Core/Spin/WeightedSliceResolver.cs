using System;
using System.Collections.Generic;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// Weighted-random slot selection over a wheel's slices.
    /// <para>
    /// Every shipped slice carries <c>Weight = 1</c>, so the shipped behaviour is an honest uniform draw
    /// (1/8 per slot, and therefore a 12.5% bomb on a normal wheel). The weight is exposed anyway so the
    /// odds can be retuned from the Inspector without touching code — and so this class has something
    /// worth unit-testing beyond "returns a number in range".
    /// </para>
    /// </summary>
    public sealed class WeightedSliceResolver : ISliceResolver
    {
        private readonly IRandomProvider _random;

        public WeightedSliceResolver(IRandomProvider random) =>
            _random = random ?? throw new ArgumentNullException(nameof(random));

        public int Resolve(IReadOnlyList<WheelSlice> slices)
        {
            if (slices == null) throw new ArgumentNullException(nameof(slices));
            if (slices.Count == 0)
                throw new ArgumentException("Cannot resolve a spin against an empty wheel.", nameof(slices));

            int totalWeight = 0;
            for (int i = 0; i < slices.Count; i++)
            {
                int weight = slices[i].Weight;
                if (weight < 0)
                    throw new ArgumentException($"Slice {i} has a negative weight ({weight}).", nameof(slices));
                totalWeight += weight;
            }

            if (totalWeight <= 0)
                throw new ArgumentException("Total slice weight must be positive; every slice was weight 0.", nameof(slices));

            int roll = _random.Next(totalWeight);
            int cumulative = 0;

            for (int i = 0; i < slices.Count; i++)
            {
                cumulative += slices[i].Weight;
                if (roll < cumulative) return i;
            }

            // Unreachable while the provider honours its contract (roll < totalWeight). Falling back to the
            // last positively-weighted slice is safer than returning an index a zero-weight slice occupies.
            for (int i = slices.Count - 1; i >= 0; i--)
                if (slices[i].Weight > 0) return i;

            throw new InvalidOperationException("Weighted resolution failed despite a positive total weight.");
        }
    }
}
