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
        private readonly bool _shuffle;

        public StubBlueprintProvider(int bombIndex = 0, bool shuffle = false)
        {
            _bombIndex = bombIndex;
            _shuffle = shuffle;
        }

        /// <summary>Set to 0 to make normal zones survivable, for long-run and overflow tests.</summary>
        public int BombWeight { get; set; } = 1;

        public WheelBlueprint GetBlueprint(int zone, ZoneType zoneType)
        {
            var slices = new List<SliceBlueprint>(WheelModel.STANDARD_SLICE_COUNT);

            for (int i = 0; i < WheelModel.STANDARD_SLICE_COUNT; i++)
            {
                bool isBomb = zoneType == ZoneType.Normal && i == _bombIndex;

                slices.Add(isBomb
                    ? SliceBlueprint.CreateBomb(BombWeight)
                    : SliceBlueprint.CreateReward(RewardFor(zoneType), BaseAmountFor(zoneType), weight: 1, unitValue: 2));
            }

            return new WheelBlueprint(TierFor(zoneType), slices, _shuffle);
        }

        private static RewardId RewardFor(ZoneType zoneType)
        {
            return zoneType == ZoneType.Super ? TestWheels.Gold
                : zoneType == ZoneType.Safe ? TestWheels.Rifle
                : TestWheels.Pistol;
        }

        private static int BaseAmountFor(ZoneType zoneType)
        {
            return zoneType == ZoneType.Super ? 100
                : zoneType == ZoneType.Safe ? 20
                : 10;
        }

        private static WheelTier TierFor(ZoneType zoneType)
        {
            return zoneType == ZoneType.Super ? WheelTier.Golden
                : zoneType == ZoneType.Safe ? WheelTier.Silver
                : WheelTier.Bronze;
        }
    }
}
