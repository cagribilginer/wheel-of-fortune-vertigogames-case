using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>Player-side RNG. Tests swap in a seeded provider through the same interface.</summary>
    public sealed class UnityRandomProvider : IRandomProvider
    {
        public int Next(int maxExclusive) => UnityEngine.Random.Range(0, maxExclusive);

        public double NextDouble() => UnityEngine.Random.value;
    }
}
