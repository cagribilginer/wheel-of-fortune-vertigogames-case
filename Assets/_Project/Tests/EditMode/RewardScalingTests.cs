using System;
using NUnit.Framework;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Tests.EditMode
{
    [TestFixture]
    public sealed class RewardScalingTests
    {
        private LinearRewardScaling _linear;

        [SetUp]
        public void SetUp() => _linear = new LinearRewardScaling();

        [Test]
        public void Linear_Zone1_PaysTheAuthoredAmount() =>
            Assert.That(_linear.Scale(100, 1), Is.EqualTo(100));

        [TestCase(10, 3.25d)]  // 1 + 0.25 * 9
        [TestCase(29, 8.00d)]  // 1 + 0.25 * 28
        public void Linear_GrowsAsDocumented(int zone, double expectedMultiplier) =>
            Assert.That(_linear.Scale(100, zone), Is.EqualTo((int)Math.Ceiling(100 * expectedMultiplier)));

        [Test]
        public void Linear_IsMonotonicOverALongRun()
        {
            int previous = 0;
            for (int zone = 1; zone <= 500; zone++)
            {
                int current = _linear.Scale(7, zone);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous), $"Regressed at zone {zone}.");
                previous = current;
            }
        }

        [Test]
        public void Linear_NeverReturnsZeroOrNegative()
        {
            for (int zone = 1; zone <= 500; zone++)
                Assert.That(_linear.Scale(1, zone), Is.GreaterThan(0), $"Non-positive at zone {zone}.");
        }

        /// <summary>Endless progression means deep zones are reachable; the cast must clamp, never wrap.</summary>
        [Test]
        public void Linear_ClampsInsteadOfOverflowing() =>
            Assert.That(_linear.Scale(int.MaxValue, 500), Is.EqualTo(int.MaxValue));

        [Test]
        public void Linear_ZeroGrowth_IsFlat()
        {
            var flat = new LinearRewardScaling(0d);
            Assert.That(flat.Scale(42, 1), Is.EqualTo(42));
            Assert.That(flat.Scale(42, 250), Is.EqualTo(42));
        }

        [Test]
        public void Linear_NegativeGrowth_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearRewardScaling(-0.1d));

        [Test]
        public void Step_ChangesOnlyAtStepBoundaries()
        {
            var step = new StepRewardScaling(zonesPerStep: 5, multiplierPerStep: 2d);

            for (int zone = 1; zone <= 5; zone++)
                Assert.That(step.Scale(10, zone), Is.EqualTo(10), $"Zone {zone} should still be in the first band.");

            Assert.That(step.Scale(10, 6), Is.EqualTo(20));
            Assert.That(step.Scale(10, 10), Is.EqualTo(20));
            Assert.That(step.Scale(10, 11), Is.EqualTo(40));
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void NonPositiveZone_Throws(int zone) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _linear.Scale(10, zone));

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveBaseAmount_Throws(int baseAmount) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _linear.Scale(baseAmount, 1));
    }
}
