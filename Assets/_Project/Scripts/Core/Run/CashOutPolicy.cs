using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The single authority on when the player may walk away:
    /// <em>"The player can leave with their haul whenever the wheel is idle, there is something banked,
    /// and the zone is safe or the super zone."</em> The zone gate is what makes reaching a safe/super
    /// zone matter — without it, cashing out never required surviving to one.
    /// <para>
    /// The EXIT button's interactable state is a <em>reflection</em> of this policy, never a second
    /// implementation of it. That is why the rule is a pure function with unit tests rather than a
    /// condition scattered across a presenter.
    /// </para>
    /// </summary>
    public static class CashOutPolicy
    {
        public static bool CanLeave(RunPhase phase, bool bankHasRewards, ZoneType zoneType) =>
            phase == RunPhase.Idle && bankHasRewards && (zoneType == ZoneType.Safe || zoneType == ZoneType.Super);

        public static bool CanSpin(RunPhase phase) => phase == RunPhase.Idle;
    }
}
