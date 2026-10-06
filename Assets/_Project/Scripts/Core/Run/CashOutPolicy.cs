using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The single authority on when the player may walk away: something banked and a safe or super zone.
    /// That the wheel is idle is the state machine's rule, and the EXIT button mirrors this policy.
    /// </summary>
    public static class CashOutPolicy
    {
        public static bool CanLeave(bool bankHasRewards, ZoneType zoneType)
        {
            return bankHasRewards && (zoneType == ZoneType.Safe || zoneType == ZoneType.Super);
        }
    }
}
