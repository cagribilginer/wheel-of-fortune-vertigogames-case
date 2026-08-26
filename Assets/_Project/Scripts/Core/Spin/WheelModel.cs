using System;
using System.Collections.Generic;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// The runtime wheel for one zone: a fixed set of slices whose amounts are already scaled.
    /// Built fresh by the factory on every zone change and never mutated afterwards.
    /// </summary>
    public sealed class WheelModel
    {
        public const int StandardSliceCount = 8;

        private readonly WheelSlice[] _slices;

        public WheelModel(WheelTier tier, IReadOnlyList<WheelSlice> slices)
        {
            if (slices == null) throw new ArgumentNullException(nameof(slices));
            if (slices.Count == 0) throw new ArgumentException("A wheel needs at least one slice.", nameof(slices));

            _slices = new WheelSlice[slices.Count];
            int totalWeight = 0;
            int bombCount = 0;

            for (int i = 0; i < slices.Count; i++)
            {
                WheelSlice slice = slices[i];
                _slices[i] = slice;
                totalWeight += slice.Weight;
                if (slice.IsBomb) bombCount++;
            }

            if (totalWeight <= 0)
                throw new ArgumentException("At least one slice must carry a positive weight.", nameof(slices));

            Tier = tier;
            TotalWeight = totalWeight;
            BombCount = bombCount;
        }

        public WheelTier Tier { get; }

        public IReadOnlyList<WheelSlice> Slices => _slices;

        public int SliceCount => _slices.Length;

        public int TotalWeight { get; }

        public int BombCount { get; }

        public bool HasBomb => BombCount > 0;

        public WheelSlice this[int index] => _slices[index];
    }
}
