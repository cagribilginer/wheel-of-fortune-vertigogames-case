using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The single authority on when the player may walk away:
    /// <em>"Player can choose to leave when wheel is not spinning and when the zone is safe or at the super zone."</em>
    /// <para>
    /// The Collect button's interactable state is a <em>reflection</em> of this policy, never a second
    /// implementation of it. That is why the rule is a pure function with unit tests rather than a
    /// condition scattered across a presenter.
    /// </para>
    /// </summary>
    public static class CashOutPolicy
    {
        public static bool CanLeave(ZoneType zoneType, RunPhase phase)
        {
            if (phase != RunPhase.Idle) return false;

            return zoneType == ZoneType.Safe || zoneType == ZoneType.Super;
        }

        /// <summary>
        /// Giving up is allowed in any idle zone — it is the "I am in a bad spot" exit — but unlike
        /// collecting it forfeits the bank, so it is never a free reroll.
        /// </summary>
        public static bool CanGiveUp(RunPhase phase) => phase == RunPhase.Idle;

        public static bool CanSpin(RunPhase phase) => phase == RunPhase.Idle;
    }
}
