namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// The randomness seam. Production uses the engine RNG; tests inject a seeded or fully scripted one,
    /// which is what makes spin resolution deterministically testable.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Returns a non-negative value strictly less than <paramref name="maxExclusive"/>.</summary>
        int Next(int maxExclusive);

        /// <summary>Returns a value in [0, 1).</summary>
        double NextDouble();
    }
}
