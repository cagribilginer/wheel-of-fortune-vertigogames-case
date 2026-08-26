using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Tests.EditMode.Doubles;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class WheelModelTests
    {
        [Test]
        public void StandardSliceCount_MatchesTheRevolverArt() =>
            Assert.That(WheelModel.StandardSliceCount, Is.EqualTo(8),
                "The provided wheel sprite is a cylinder with exactly eight slots.");

        [Test]
        public void NormalWheel_HasExactlyOneBomb()
        {
            var wheel = TestWheels.NormalWheel();

            Assert.That(wheel.SliceCount, Is.EqualTo(8));
            Assert.That(wheel.BombCount, Is.EqualTo(1));
            Assert.That(wheel.HasBomb, Is.True);
        }

        [Test]
        public void SafeWheel_HasNoBomb()
        {
            var wheel = TestWheels.SafeWheel();

            Assert.That(wheel.SliceCount, Is.EqualTo(8));
            Assert.That(wheel.BombCount, Is.Zero);
            Assert.That(wheel.HasBomb, Is.False);
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(7)]
        public void BombCanSitInAnySlot(int bombIndex)
        {
            var wheel = TestWheels.NormalWheel(bombIndex);

            Assert.That(wheel[bombIndex].IsBomb, Is.True);
            Assert.That(wheel.BombCount, Is.EqualTo(1));
        }

        [Test]
        public void TotalWeight_SumsSliceWeights() =>
            Assert.That(TestWheels.NormalWheel().TotalWeight, Is.EqualTo(8));

        [Test]
        public void EmptySliceList_Throws() =>
            Assert.Throws<ArgumentException>(() => new WheelModel(WheelTier.Bronze, new List<WheelSlice>()));

        [Test]
        public void NullSliceList_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new WheelModel(WheelTier.Bronze, null));

        [Test]
        public void AllZeroWeights_Throws() =>
            Assert.Throws<ArgumentException>(() =>
                new WheelModel(WheelTier.Bronze, TestWheels.WeightedSlices(0, 0, 0)));

        [Test]
        public void RewardSlice_RequiresANonEmptyRewardId() =>
            Assert.Throws<ArgumentException>(() =>
                WheelSlice.CreateReward(Vertigo.Wheel.Core.Rewards.RewardId.None, 5));

        [TestCase(0)]
        [TestCase(-1)]
        public void RewardSlice_RequiresAPositiveAmount(int amount) =>
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WheelSlice.CreateReward(TestWheels.Pistol, amount));

        [Test]
        public void RewardSlice_RejectsNegativeWeight() =>
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WheelSlice.CreateReward(TestWheels.Pistol, 1, weight: -1));

        [Test]
        public void BombSlice_CarriesNoReward()
        {
            WheelSlice bomb = WheelSlice.CreateBomb();

            Assert.That(bomb.IsBomb, Is.True);
            Assert.That(bomb.Reward.IsEmpty, Is.True);
            Assert.That(bomb.Amount, Is.Zero);
        }

        [Test]
        public void SpinOutcome_MirrorsTheLandedSlice()
        {
            var wheel = TestWheels.NormalWheel(bombIndex: 2, amount: 25);
            var service = new SpinService(new FixedSliceResolver(5));

            SpinOutcome outcome = service.Spin(wheel);

            Assert.That(outcome.SlotIndex, Is.EqualTo(5));
            Assert.That(outcome.IsBomb, Is.False);
            Assert.That(outcome.Amount, Is.EqualTo(25));
            Assert.That(outcome.Reward, Is.EqualTo(TestWheels.Pistol));
        }

        [Test]
        public void SpinOutcome_ReportsABombLanding()
        {
            var wheel = TestWheels.NormalWheel(bombIndex: 2);
            var service = new SpinService(new FixedSliceResolver(2));

            Assert.That(service.Spin(wheel).IsBomb, Is.True);
        }

        [Test]
        public void SpinService_RejectsANullWheel() =>
            Assert.Throws<ArgumentNullException>(() => new SpinService(new FixedSliceResolver(0)).Spin(null));
    }
}
