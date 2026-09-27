using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The single authority on when the player may walk away: the wheel must be idle, something must be
    /// banked, and the zone must be safe or super. The EXIT button's interactable state mirrors this
    /// policy rather than reimplementing it.
    /// </summary>
    public static class CashOutPolicy
    {
        public static bool CanLeave(RunPhase phase, bool bankHasRewards, ZoneType zoneType)
        {
            return phase == RunPhase.Idle && bankHasRewards && (zoneType == ZoneType.Safe || zoneType == ZoneType.Super);
        }

        public static bool CanSpin(RunPhase phase)
        {
            return phase == RunPhase.Idle;
        }
    }
}
