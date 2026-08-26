using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>
    /// Stands in for the ZoneProgressionConfig asset: normal zones get seven rewards plus a bomb,
    /// safe and super zones get eight rewards and a richer pool.
    /// </summary>
    public sealed class StubBlueprintProvider : IWheelBlueprintProvider
    {
        private readonly int _bombIndex;

        public StubBlueprintProvider(int bombIndex = 0) => _bombIndex = bombIndex;

        /// <summary>Set to 0 to make normal zones survivable, for long-run and overflow tests.</summary>
        public int BombWeight { get; set; } = 1;

        public WheelBlueprint GetBlueprint(int zone, ZoneType zoneType)
        {
            var slices = new List<SliceBlueprint>(WheelModel.StandardSliceCount);

            for (int i = 0; i < WheelModel.StandardSliceCount; i++)
            {
                bool isBomb = zoneType == ZoneType.Normal && i == _bombIndex;

                slices.Add(isBomb
                    ? SliceBlueprint.CreateBomb(BombWeight)
                    : SliceBlueprint.CreateReward(RewardFor(zoneType), BaseAmountFor(zoneType), weight: 1, unitValue: 2));
            }

            return new WheelBlueprint(TierFor(zoneType), slices);
        }

        private static RewardId RewardFor(ZoneType zoneType) =>
            zoneType == ZoneType.Super ? TestWheels.Gold
            : zoneType == ZoneType.Safe ? TestWheels.Rifle
            : TestWheels.Pistol;

        private static int BaseAmountFor(ZoneType zoneType) =>
            zoneType == ZoneType.Super ? 100
            : zoneType == ZoneType.Safe ? 20
            : 10;

        private static WheelTier TierFor(ZoneType zoneType) =>
            zoneType == ZoneType.Super ? WheelTier.Golden
            : zoneType == ZoneType.Safe ? WheelTier.Silver
            : WheelTier.Bronze;
    }
}
