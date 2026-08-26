using System;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>
    /// Replays a fixed queue of rolls, so a test can state exactly which slot a spin lands on
    /// without reasoning about the resolver's internals.
    /// </summary>
    public sealed class ScriptedRandomProvider : IRandomProvider
    {
        private readonly int[] _rolls;
        private int _cursor;

        public ScriptedRandomProvider(params int[] rolls)
        {
            if (rolls == null || rolls.Length == 0)
                throw new ArgumentException("Provide at least one scripted roll.", nameof(rolls));

            _rolls = rolls;
        }

        public int Next(int maxExclusive)
        {
            int roll = _rolls[_cursor % _rolls.Length];
            _cursor++;
            return roll % maxExclusive;
        }

        public double NextDouble() => Next(1000) / 1000d;
    }
}
