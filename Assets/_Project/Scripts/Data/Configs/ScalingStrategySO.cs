using UnityEngine;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>
    /// Strategy as an asset. Dropping a different scaling asset into the progression config changes the
    /// entire reward economy with no recompilation, which is the whole reason this is an interface rather
    /// than a constant.
    /// </summary>
    public abstract class ScalingStrategySO : ScriptableObject, IRewardScaling
    {
        public abstract int Scale(int baseAmount, int zone);
    }
}
