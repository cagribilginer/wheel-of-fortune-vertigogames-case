using NUnit.Framework;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// "Player can choose to leave when wheel is not spinning and when the zone is safe or at the super zone."
    /// Both halves of that sentence are tested here, because the Collect button only mirrors this decision.
    /// </summary>
    [TestFixture]
    public sealed class CashOutPolicyTests
    {
        [TestCase(ZoneType.Safe, RunPhase.Idle, true)]
        [TestCase(ZoneType.Super, RunPhase.Idle, true)]
        [TestCase(ZoneType.Normal, RunPhase.Idle, false)]
        public void ZoneType_GatesLeaving(ZoneType zone, RunPhase phase, bool expected) =>
            Assert.That(CashOutPolicy.CanLeave(zone, phase), Is.EqualTo(expected));

        [TestCase(RunPhase.Spinning)]
        [TestCase(RunPhase.Resolving)]
        [TestCase(RunPhase.GameOver)]
        [TestCase(RunPhase.CashOut)]
        public void NonIdlePhase_BlocksLeavingEvenOnASafeZone(RunPhase phase)
        {
            Assert.That(CashOutPolicy.CanLeave(ZoneType.Safe, phase), Is.False);
            Assert.That(CashOutPolicy.CanLeave(ZoneType.Super, phase), Is.False);
        }

        [TestCase(RunPhase.Idle, true)]
        [TestCase(RunPhase.Spinning, false)]
        [TestCase(RunPhase.Resolving, false)]
        [TestCase(RunPhase.GameOver, false)]
        public void Spinning_IsOnlyAllowedWhenIdle(RunPhase phase, bool expected) =>
            Assert.That(CashOutPolicy.CanSpin(phase), Is.EqualTo(expected));

        /// <summary>Giving up is available on any idle zone — unlike collecting, it costs the haul.</summary>
        [TestCase(RunPhase.Idle, true)]
        [TestCase(RunPhase.Spinning, false)]
        public void GivingUp_IsAllowedOnAnyIdleZone(RunPhase phase, bool expected) =>
            Assert.That(CashOutPolicy.CanGiveUp(phase), Is.EqualTo(expected));
    }
}
