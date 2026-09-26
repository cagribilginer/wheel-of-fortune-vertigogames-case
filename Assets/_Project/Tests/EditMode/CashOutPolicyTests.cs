using NUnit.Framework;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// "The player can leave with their haul whenever the wheel is idle, there is something banked, and
    /// the zone is safe or the super zone." All three conditions are tested here, because the EXIT
    /// button only mirrors this decision.
    /// </summary>
    [TestFixture]
    public sealed class CashOutPolicyTests
    {
        [TestCase(RunPhase.Idle, true, ZoneType.Safe, true)]
        [TestCase(RunPhase.Idle, true, ZoneType.Super, true)]
        [TestCase(RunPhase.Idle, false, ZoneType.Safe, false)]
        public void LeavingNeedsAnIdleWheelWithAHaulOnASafeOrSuperZone(
            RunPhase phase, bool bankHasRewards, ZoneType zoneType, bool expected) =>
            Assert.That(CashOutPolicy.CanLeave(phase, bankHasRewards, zoneType), Is.EqualTo(expected));

        [Test]
        public void NormalZone_BlocksLeavingEvenIdleWithAHaul() =>
            Assert.That(CashOutPolicy.CanLeave(RunPhase.Idle, bankHasRewards: true, ZoneType.Normal), Is.False);

        [TestCase(RunPhase.Spinning)]
        [TestCase(RunPhase.Resolving)]
        [TestCase(RunPhase.GameOver)]
        [TestCase(RunPhase.CashOut)]
        public void NonIdlePhase_BlocksLeavingEvenWithAHaulOnASafeZone(RunPhase phase) =>
            Assert.That(CashOutPolicy.CanLeave(phase, bankHasRewards: true, ZoneType.Safe), Is.False);

        [TestCase(RunPhase.Idle, true)]
        [TestCase(RunPhase.Spinning, false)]
        [TestCase(RunPhase.Resolving, false)]
        [TestCase(RunPhase.GameOver, false)]
        public void Spinning_IsOnlyAllowedWhenIdle(RunPhase phase, bool expected) =>
            Assert.That(CashOutPolicy.CanSpin(phase), Is.EqualTo(expected));
    }
}
