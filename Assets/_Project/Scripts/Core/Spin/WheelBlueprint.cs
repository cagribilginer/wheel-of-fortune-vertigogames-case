using System;
using System.Collections.Generic;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// An authored wheel: its tier and its eight slices, before any zone scaling.
    /// This is the shape a <c>ZoneWheelConfig</c> asset presents to the core layer.
    /// </summary>
    public sealed class WheelBlueprint
    {
        private readonly SliceBlueprint[] _slices;

        public WheelBlueprint(WheelTier tier, IReadOnlyList<SliceBlueprint> slices, bool shuffleSlices = false)
        {
            if (slices == null) throw new ArgumentNullException(nameof(slices));
            if (slices.Count == 0) throw new ArgumentException("A wheel needs at least one slice.", nameof(slices));

            _slices = new SliceBlueprint[slices.Count];
            int bombCount = 0;

            for (int i = 0; i < slices.Count; i++)
            {
                _slices[i] = slices[i];
                if (slices[i].IsBomb) bombCount++;
            }

            Tier = tier;
            BombCount = bombCount;
            ShuffleSlices = shuffleSlices;
        }

        public WheelTier Tier { get; }

        public IReadOnlyList<SliceBlueprint> Slices => _slices;

        public int BombCount { get; }

        /// <summary>
        /// When set, the factory reorders this wheel's slices each time it builds a zone, so the same reward
        /// pool does not sit on the same wedges every zone. Bomb <em>count</em> is unaffected — only its
        /// position moves — so the safe/super risk-free rule still holds.
        /// </summary>
        public bool ShuffleSlices { get; }
    }
}
