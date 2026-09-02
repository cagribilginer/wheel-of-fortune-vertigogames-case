using NUnit.Framework;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Tests.EditMode.Doubles;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class SliceBlueprintTests
    {
        private static readonly RewardId Knife = new RewardId("knife_points");
        private static readonly RewardId Cash = new RewardId("cash");

        [Test]
        public void StackableSlice_ScalesWithZoneDepth()
        {
            var slice = SliceBlueprint.CreateReward(Cash, baseAmount: 10, scalable: true);

            // zone 29 with the default linear curve is 8x (1 + 0.25 * 28).
            Assert.That(slice.ToSlice(29, new LinearRewardScaling()).Amount, Is.EqualTo(80));
        }

        [TestCase(2)]
        [TestCase(15)]
        [TestCase(99)]
        public void NonStackableSlice_IgnoresZoneScalingEntirely(int zone)
        {
            var slice = SliceBlueprint.CreateReward(Knife, baseAmount: 1, scalable: false);

            Assert.That(slice.ToSlice(zone, new LinearRewardScaling()).Amount, Is.EqualTo(1));
        }

        [Test]
        public void NonStackableSlice_KeepsItsBaseAmountEvenIfAuthoredAboveOne()
        {
            // The guarantee is "scaling never touches it", not "it is silently clamped": the clamp to 1
            // lives in WheelSliceEntry, which is what every wheel actually goes through.
            var slice = SliceBlueprint.CreateReward(Knife, baseAmount: 3, scalable: false);

            Assert.That(slice.ToSlice(50, new LinearRewardScaling()).Amount, Is.EqualTo(3));
        }

        [Test]
        public void RewardSlicesDefaultToScalable()
        {
            Assert.That(SliceBlueprint.CreateReward(Cash, 5).Scalable, Is.True);
        }

        [Test]
        public void BombSlice_IsNeverScalable()
        {
            Assert.That(SliceBlueprint.CreateBomb().Scalable, Is.False);
        }

        [Test]
        public void IdentityScaling_LeavesAStackableAmountUntouched()
        {
            var slice = SliceBlueprint.CreateReward(Cash, baseAmount: 12, scalable: true);

            Assert.That(slice.ToSlice(7, new IdentityScaling()).Amount, Is.EqualTo(12));
        }

        [Test]
        public void ShardSlice_RampsUpButNeverExceedsItsCeiling()
        {
            var shard = SliceBlueprint.CreateReward(Knife, baseAmount: 1, scalable: true, maxAmount: 5);
            var curve = new LinearRewardScaling();

            Assert.That(shard.ToSlice(1, curve).Amount, Is.EqualTo(1), "zone 1 pays the base");
            Assert.That(shard.ToSlice(6, curve).Amount, Is.EqualTo(3), "still ramping mid-run");
            Assert.That(shard.ToSlice(15, curve).Amount, Is.EqualTo(5), "reaches the ceiling");
            Assert.That(shard.ToSlice(99, curve).Amount, Is.EqualTo(5), "and stays there however deep");
        }

        [Test]
        public void AZeroCeiling_MeansNoCeiling()
        {
            var slice = SliceBlueprint.CreateReward(Cash, baseAmount: 10, scalable: true, maxAmount: 0);

            Assert.That(slice.ToSlice(29, new LinearRewardScaling()).Amount, Is.EqualTo(80));
        }

        [Test]
        public void ACeilingBelowTheBaseAmount_IsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SliceBlueprint.CreateReward(Knife, baseAmount: 6, maxAmount: 5));
        }
    }
}
