using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>Small builders so wheel-shaped fixtures do not dominate the tests that use them.</summary>
    public static class TestWheels
    {
        public static readonly RewardId Pistol = new RewardId("pistol_points");
        public static readonly RewardId Rifle = new RewardId("rifle_points");
        public static readonly RewardId Gold = new RewardId("gold");

        /// <summary>Seven reward slices plus one bomb at <paramref name="bombIndex"/>, all weight 1.</summary>
        public static WheelModel NormalWheel(int bombIndex = 0, int amount = 10)
        {
            var slices = new List<WheelSlice>(WheelModel.StandardSliceCount);
            for (int i = 0; i < WheelModel.StandardSliceCount; i++)
            {
                slices.Add(i == bombIndex
                    ? WheelSlice.CreateBomb()
                    : WheelSlice.CreateReward(Pistol, amount));
            }

            return new WheelModel(WheelTier.Bronze, slices);
        }

        /// <summary>Eight reward slices, no bomb.</summary>
        public static WheelModel SafeWheel(int amount = 15)
        {
            var slices = new List<WheelSlice>(WheelModel.StandardSliceCount);
            for (int i = 0; i < WheelModel.StandardSliceCount; i++)
                slices.Add(WheelSlice.CreateReward(Rifle, amount));

            return new WheelModel(WheelTier.Silver, slices);
        }

        public static List<WheelSlice> WeightedSlices(params int[] weights)
        {
            var slices = new List<WheelSlice>(weights.Length);
            for (int i = 0; i < weights.Length; i++)
                slices.Add(WheelSlice.CreateReward(Pistol, 1, weights[i]));

            return slices;
        }
    }
}
