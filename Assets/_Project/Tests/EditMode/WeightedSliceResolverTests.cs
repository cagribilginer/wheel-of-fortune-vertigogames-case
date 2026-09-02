using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Tests.EditMode.Doubles;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class WeightedSliceResolverTests
    {
        private const int SampleCount = 100_000;

        private static WeightedSliceResolver Seeded(int seed) =>
            new WeightedSliceResolver(new SystemRandomProvider(seed));

        [Test]
        public void SingleNonZeroWeight_AlwaysWins()
        {
            var slices = TestWheels.WeightedSlices(0, 0, 0, 1, 0, 0, 0, 0);
            var resolver = Seeded(1);

            for (int i = 0; i < 500; i++)
                Assert.That(resolver.Resolve(slices), Is.EqualTo(3));
        }

        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var slices = TestWheels.WeightedSlices(1, 1, 1, 1, 1, 1, 1, 1);
            var first = new List<int>();
            var second = new List<int>();

            var a = Seeded(12345);
            var b = Seeded(12345);

            for (int i = 0; i < 1000; i++)
            {
                first.Add(a.Resolve(slices));
                second.Add(b.Resolve(slices));
            }

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void UniformWeights_ProduceUniformDistribution()
        {
            var slices = TestWheels.WeightedSlices(1, 1, 1, 1, 1, 1, 1, 1);
            var counts = Sample(slices, SampleCount, seed: 2024);

            for (int i = 0; i < counts.Length; i++)
            {
                double share = counts[i] / (double)SampleCount;
                Assert.That(share, Is.EqualTo(0.125d).Within(0.02d),
                    $"Slot {i} drew {share:P2}, expected ~12.5%.");
            }
        }

        /// <summary>The bomb is one slot of eight at equal weight, i.e. an honest 12.5%.</summary>
        [Test]
        public void UniformEightSliceWheel_GivesBombOneInEight()
        {
            var wheel = TestWheels.NormalWheel(bombIndex: 3);
            var service = new SpinService(Seeded(7));

            int bombs = 0;
            for (int i = 0; i < SampleCount; i++)
                if (service.Spin(wheel).IsBomb) bombs++;

            Assert.That(bombs / (double)SampleCount, Is.EqualTo(0.125d).Within(0.02d));
        }

        /// <summary>
        /// End-to-end sanity on the shipped bronze wheel: build one through the real factory (7 rewards +
        /// one weight-1 bomb, exactly as Wheel_Bronze_Band1/2/3.asset author it), spin it 1000 times and
        /// confirm the bomb lands ~12.5% of the time — i.e. no pity/mercy rule is quietly holding it back.
        /// </summary>
        [Test]
        public void BronzeWheel_SpunAThousandTimes_DetonatesRoughlyOneInEight()
        {
            var factory = new ZoneWheelFactory(
                new Vertigo.Wheel.Core.Zones.ZoneClassifier(),
                new StubBlueprintProvider(bombIndex: 0),
                new Vertigo.Wheel.Core.Rewards.LinearRewardScaling());
            WheelModel bronze = factory.Build(2); // zone 2: a normal bronze zone

            Assume.That(bronze.Tier, Is.EqualTo(WheelTier.Bronze));
            Assume.That(bronze.BombCount, Is.EqualTo(1));

            var service = new SpinService(Seeded(4242));

            const int spins = 1000;
            int bombs = 0;
            for (int i = 0; i < spins; i++)
                if (service.Spin(bronze).IsBomb) bombs++;

            double frequency = bombs / (double)spins;
            TestContext.Out.WriteLine(
                $"Bronze wheel bomb frequency over {spins} spins: {bombs}/{spins} = {frequency:P2} (ideal 12.50%).");

            Assert.That(frequency, Is.EqualTo(0.125d).Within(0.04d));
        }

        [Test]
        public void HeavilyWeightedSlot_SkewsProportionally()
        {
            var slices = TestWheels.WeightedSlices(1, 1, 1, 10, 1, 1, 1, 1);
            var counts = Sample(slices, SampleCount, seed: 99);

            double expected = 10d / 17d;
            Assert.That(counts[3] / (double)SampleCount, Is.EqualTo(expected).Within(0.01d));
        }

        [Test]
        public void EveryWeightZero_Throws()
        {
            var slices = TestWheels.WeightedSlices(0, 0, 0, 0);
            Assert.Throws<ArgumentException>(() => Seeded(1).Resolve(slices));
        }

        [Test]
        public void EmptySliceList_Throws() =>
            Assert.Throws<ArgumentException>(() => Seeded(1).Resolve(new List<WheelSlice>()));

        [Test]
        public void NullSliceList_Throws() =>
            Assert.Throws<ArgumentNullException>(() => Seeded(1).Resolve(null));

        [Test]
        public void NullRandomProvider_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new WeightedSliceResolver(null));

        private static int[] Sample(IReadOnlyList<WheelSlice> slices, int draws, int seed)
        {
            var resolver = Seeded(seed);
            var counts = new int[slices.Count];

            for (int i = 0; i < draws; i++)
                counts[resolver.Resolve(slices)]++;

            return counts;
        }
    }
}
