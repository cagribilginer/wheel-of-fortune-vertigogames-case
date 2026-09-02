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
    }
}
