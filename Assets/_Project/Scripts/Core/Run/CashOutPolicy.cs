using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The single authority on when the player may walk away: something must be banked and the zone must
    /// be safe or super. That the wheel is idle is the state machine's rule (only <c>IdleState</c> handles the
    /// exit input), so it is not restated here. The EXIT button's interactable state mirrors this policy
    /// rather than reimplementing it.
    /// </summary>
    public static class CashOutPolicy
    {
        public static bool CanLeave(bool bankHasRewards, ZoneType zoneType)
        {
            return bankHasRewards && (zoneType == ZoneType.Safe || zoneType == ZoneType.Super);
        }
    }
}
