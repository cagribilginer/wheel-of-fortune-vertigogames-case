using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>Small builders so wheel-shaped fixtures do not dominate the tests that use them.</summary>
    public static class TestWheels
    {
        public static readonly RewardId Pistol = new("pistol_points");
        public static readonly RewardId Rifle = new("rifle_points");
        public static readonly RewardId Gold = new("gold");
        public static readonly RewardId Cash = new("cash");

        /// <summary>The wallet currencies a test run is built with, in display order.</summary>
        public static readonly RewardId[] Currencies = { Gold, Cash };

        /// <summary>The revive pricing the tests run against: 50 gold + 10 per zone, one free ad revive.</summary>
        public static readonly ContinueSettings Continue = new(50, 10, 1);

        /// <summary>Seven reward slices plus one bomb at <paramref name="bombIndex"/>, all weight 1.</summary>
        public static WheelModel NormalWheel(int bombIndex = 0, int amount = 10)
        {
            var slices = new List<WheelSlice>(WheelModel.STANDARD_SLICE_COUNT);
            for (int i = 0; i < WheelModel.STANDARD_SLICE_COUNT; i++)
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
            var slices = new List<WheelSlice>(WheelModel.STANDARD_SLICE_COUNT);
            for (int i = 0; i < WheelModel.STANDARD_SLICE_COUNT; i++)
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
