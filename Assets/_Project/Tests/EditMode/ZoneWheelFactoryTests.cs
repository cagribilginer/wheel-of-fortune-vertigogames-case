using System;
using NUnit.Framework;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Tests.EditMode.Doubles;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class ZoneWheelFactoryTests
    {
        private ZoneWheelFactory _factory;
        private StubBlueprintProvider _blueprints;

        [SetUp]
        public void SetUp()
        {
            _blueprints = new StubBlueprintProvider();
            _factory = new ZoneWheelFactory(new ZoneClassifier(), _blueprints, new LinearRewardScaling());
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(29)]
        public void NormalZone_HasExactlyOneBomb(int zone) =>
            Assert.That(_factory.Build(zone).BombCount, Is.EqualTo(1));

        [TestCase(5)]
        [TestCase(25)]
        public void SafeZone_HasNoBomb(int zone) =>
            Assert.That(_factory.Build(zone).BombCount, Is.Zero);

        [TestCase(30)]
        [TestCase(60)]
        public void SuperZone_HasNoBomb(int zone) =>
            Assert.That(_factory.Build(zone).BombCount, Is.Zero);

        [TestCase(1, WheelTier.Bronze)]
        [TestCase(5, WheelTier.Silver)]
        [TestCase(30, WheelTier.Golden)]
        public void TierFollowsZoneType(int zone, WheelTier expected) =>
            Assert.That(_factory.Build(zone).Tier, Is.EqualTo(expected));

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(30)]
        [TestCase(147)]
        public void EveryWheelHasEightSlices(int zone) =>
            Assert.That(_factory.Build(zone).SliceCount, Is.EqualTo(8));

        [Test]
        public void SliceAmounts_AreScaledForTheZone()
        {
            // Base 10 on a normal zone; zone 5 is safe, so use zone 4: 1 + 0.25 * 3 = 1.75x.
            WheelModel zone1 = _factory.Build(1);
            WheelModel zone4 = _factory.Build(4);

            int rewardAt1 = FirstRewardAmount(zone1);
            int rewardAt4 = FirstRewardAmount(zone4);

            Assert.That(rewardAt1, Is.EqualTo(10));
            Assert.That(rewardAt4, Is.EqualTo((int)Math.Ceiling(10 * 1.75d)));
        }

        [Test]
        public void ScalingIsAppliedToTheBlueprintWithoutMutatingIt()
        {
            _factory.Build(50);
            WheelModel again = _factory.Build(1);

            Assert.That(FirstRewardAmount(again), Is.EqualTo(10),
                "Building a deep zone must not have altered the authored base amount.");
        }

        [Test]
        public void UnitValue_SurvivesIntoTheBuiltSlice()
        {
            WheelModel wheel = _factory.Build(1);

            for (int i = 0; i < wheel.SliceCount; i++)
                if (!wheel[i].IsBomb)
                    Assert.That(wheel[i].UnitValue, Is.EqualTo(2));
        }

        [Test]
        public void BombSlice_CarriesNoAmountRegardlessOfZone()
        {
            WheelModel deep = _factory.Build(99);

            for (int i = 0; i < deep.SliceCount; i++)
                if (deep[i].IsBomb)
                    Assert.That(deep[i].Amount, Is.Zero);
        }

        /// <summary>
        /// A safe or super zone carrying a bomb would break the mode's headline promise, so it fails loudly
        /// at build time rather than surfacing as a bad play-through.
        /// </summary>
        [Test]
        public void SafeZoneWithABombBlueprint_ThrowsRatherThanShipping()
        {
            var bad = new AlwaysBombedProvider();
            var factory = new ZoneWheelFactory(new ZoneClassifier(), bad, new LinearRewardScaling());

            Assert.Throws<InvalidOperationException>(() => factory.Build(5));
        }

        [Test]
        public void MissingBlueprint_Throws()
        {
            var empty = new NullProvider();
            var factory = new ZoneWheelFactory(new ZoneClassifier(), empty, new LinearRewardScaling());

            Assert.Throws<InvalidOperationException>(() => factory.Build(1));
        }

        [Test]
        public void NullDependencies_Throw()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneWheelFactory(null, _blueprints, new LinearRewardScaling()));
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneWheelFactory(new ZoneClassifier(), null, new LinearRewardScaling()));
            Assert.Throws<ArgumentNullException>(() =>
                new ZoneWheelFactory(new ZoneClassifier(), _blueprints, null));
        }

        private static int FirstRewardAmount(WheelModel wheel)
        {
            for (int i = 0; i < wheel.SliceCount; i++)
                if (!wheel[i].IsBomb) return wheel[i].Amount;

            throw new InvalidOperationException("Wheel had no reward slices.");
        }

        private sealed class AlwaysBombedProvider : IWheelBlueprintProvider
        {
            public WheelBlueprint GetBlueprint(int zone, ZoneType zoneType) =>
                new WheelBlueprint(WheelTier.Silver, new[] { SliceBlueprint.CreateBomb(), SliceBlueprint.CreateReward(TestWheels.Pistol, 1) });
        }

        private sealed class NullProvider : IWheelBlueprintProvider
        {
            public WheelBlueprint GetBlueprint(int zone, ZoneType zoneType) => null;
        }
    }
}
