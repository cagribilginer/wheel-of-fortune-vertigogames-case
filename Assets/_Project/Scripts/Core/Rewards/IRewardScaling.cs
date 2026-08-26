namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// How a slice's authored base amount grows with zone depth — the "rewards get better every zone" rule.
    /// A seam rather than a constant because it is the single most designer-facing knob in the game, and
    /// swapping the curve must not require a recompile.
    /// </summary>
    public interface IRewardScaling
    {
        int Scale(int baseAmount, int zone);
    }
}
