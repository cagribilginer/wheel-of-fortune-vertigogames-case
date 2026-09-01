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

        [TestCase(2)]
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

        [TestCase(1, WheelTier.Silver)]
        [TestCase(2, WheelTier.Bronze)]
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
            // Base 10 on a normal zone (zone 1 is safe now, so start at zone 2):
            // zone 2 = 1 + 0.25 * 1 = 1.25x, zone 4 = 1 + 0.25 * 3 = 1.75x.
            WheelModel zone2 = _factory.Build(2);
            WheelModel zone4 = _factory.Build(4);

            Assert.That(FirstRewardAmount(zone2), Is.EqualTo((int)Math.Ceiling(10 * 1.25d)));
            Assert.That(FirstRewardAmount(zone4), Is.EqualTo((int)Math.Ceiling(10 * 1.75d)));
        }

        [Test]
        public void ScalingIsAppliedToTheBlueprintWithoutMutatingIt()
        {
            _factory.Build(50);
            WheelModel again = _factory.Build(2);

            Assert.That(FirstRewardAmount(again), Is.EqualTo((int)Math.Ceiling(10 * 1.25d)),
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

        [Test]
        public void AShufflingBlueprint_DoesNothingWithoutARandomProvider()
        {
            // _blueprints puts the bomb at index 0; a shuffling blueprint with no RNG stays in order.
            var shufflingButNoRng = new ZoneWheelFactory(
                new ZoneClassifier(), new StubBlueprintProvider(bombIndex: 0, shuffle: true),
                new LinearRewardScaling());

            Assert.That(shufflingButNoRng.Build(2)[0].IsBomb, Is.True);
        }

        [Test]
        public void AShufflingWheel_MovesTheBombBetweenWedgesWithoutChangingTheCount()
        {
            var factory = new ZoneWheelFactory(
                new ZoneClassifier(), new StubBlueprintProvider(bombIndex: 0, shuffle: true),
                new LinearRewardScaling(), new SystemRandomProvider(12345));

            var bombWedges = new System.Collections.Generic.HashSet<int>();
            for (int build = 0; build < 40; build++)
            {
                WheelModel wheel = factory.Build(2);
                Assert.That(wheel.BombCount, Is.EqualTo(1), "the bomb is moved, never removed or duplicated");

                for (int s = 0; s < wheel.SliceCount; s++)
                    if (wheel[s].IsBomb) bombWedges.Add(s);
            }

            Assert.That(bombWedges.Count, Is.GreaterThan(1),
                "the bomb should land on different wedges across zone builds");
        }

        [Test]
        public void AShufflingWheel_KeepsTheSameSlicePool()
        {
            WheelModel plain = _factory.Build(3);
            var shuffling = new ZoneWheelFactory(
                new ZoneClassifier(), new StubBlueprintProvider(shuffle: true), new LinearRewardScaling(),
                new SystemRandomProvider(7));

            CollectionAssert.AreEquivalent(plain.Slices, shuffling.Build(3).Slices);
        }

        [Test]
        public void AShufflingSafeZone_StaysRiskFree()
        {
            var factory = new ZoneWheelFactory(
                new ZoneClassifier(), new StubBlueprintProvider(shuffle: true), new LinearRewardScaling(),
                new SystemRandomProvider(2));

            for (int build = 0; build < 10; build++)
                Assert.That(factory.Build(5).BombCount, Is.Zero);
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
