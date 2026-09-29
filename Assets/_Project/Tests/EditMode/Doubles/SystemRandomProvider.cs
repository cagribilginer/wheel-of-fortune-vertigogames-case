using System;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>
    /// A seedable <see cref="IRandomProvider"/> backed by <see cref="Random"/>, so a test can reproduce a
    /// run exactly. Production uses the engine RNG (<c>UnityRandomProvider</c>) instead.
    /// </summary>
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider()
        {
            _random = new Random();
        }

        public SystemRandomProvider(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive < 1)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Upper bound must be >= 1.");

            return _random.Next(maxExclusive);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
