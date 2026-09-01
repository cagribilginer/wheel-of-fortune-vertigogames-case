using NUnit.Framework;
using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// "The player can leave with their haul whenever the wheel is idle and there is something banked."
    /// Both halves of that sentence are tested here, because the EXIT button only mirrors this decision.
    /// </summary>
    [TestFixture]
    public sealed class CashOutPolicyTests
    {
        [TestCase(RunPhase.Idle, true, true)]
        [TestCase(RunPhase.Idle, false, false)]
        public void LeavingNeedsAnIdleWheelWithAHaul(RunPhase phase, bool bankHasRewards, bool expected) =>
            Assert.That(CashOutPolicy.CanLeave(phase, bankHasRewards), Is.EqualTo(expected));

        [TestCase(RunPhase.Spinning)]
        [TestCase(RunPhase.Resolving)]
        [TestCase(RunPhase.GameOver)]
        [TestCase(RunPhase.CashOut)]
        public void NonIdlePhase_BlocksLeavingEvenWithAHaul(RunPhase phase) =>
            Assert.That(CashOutPolicy.CanLeave(phase, bankHasRewards: true), Is.False);

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
