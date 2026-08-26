using System;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// Default <see cref="IRandomProvider"/> backed by <see cref="Random"/>.
    /// Accepts an optional seed so a run can be reproduced exactly — used by the golden-run test.
    /// </summary>
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider() => _random = new Random();

        public SystemRandomProvider(int seed) => _random = new Random(seed);

        public int Next(int maxExclusive)
        {
            if (maxExclusive < 1)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Upper bound must be >= 1.");

            return _random.Next(maxExclusive);
        }

        public double NextDouble() => _random.NextDouble();
    }
}
