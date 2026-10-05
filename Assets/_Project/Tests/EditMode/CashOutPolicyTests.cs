using NUnit.Framework;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Tests.EditMode
{
    /// <summary>
    /// "The player can leave with their haul whenever something is banked and the zone is safe or super."
    /// The EXIT button only mirrors this decision; that the wheel is idle is the state machine's rule.
    /// </summary>
    [TestFixture]
    public sealed class CashOutPolicyTests
    {
        [TestCase(true, ZoneType.Safe, true)]
        [TestCase(true, ZoneType.Super, true)]
        [TestCase(false, ZoneType.Safe, false)]
        [TestCase(false, ZoneType.Super, false)]
        [TestCase(true, ZoneType.Normal, false)]
        [TestCase(false, ZoneType.Normal, false)]
        public void LeavingNeedsAHaulOnASafeOrSuperZone(bool bankHasRewards, ZoneType zoneType, bool expected)
        {
            Assert.That(CashOutPolicy.CanLeave(bankHasRewards, zoneType), Is.EqualTo(expected));
        }
    }
}
