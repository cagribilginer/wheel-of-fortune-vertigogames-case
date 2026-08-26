using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>Pass-through scaling, so tests about other rules are not perturbed by zone growth.</summary>
    public sealed class IdentityScaling : IRewardScaling
    {
        public int Scale(int baseAmount, int zone) => baseAmount;
    }
}
