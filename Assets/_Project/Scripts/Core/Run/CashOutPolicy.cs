namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The single authority on when the player may walk away:
    /// <em>"The player can leave with their haul whenever the wheel is idle and there is something banked."</em>
    /// Leaving is no longer gated on the zone type — a spin is only ever reached from zone one (which is
    /// safe) by first banking a reward, so the bank is the real precondition, not the zone.
    /// <para>
    /// The EXIT button's interactable state is a <em>reflection</em> of this policy, never a second
    /// implementation of it. That is why the rule is a pure function with unit tests rather than a
    /// condition scattered across a presenter.
    /// </para>
    /// </summary>
    public static class CashOutPolicy
    {
        public static bool CanLeave(RunPhase phase, bool bankHasRewards) =>
            phase == RunPhase.Idle && bankHasRewards;

        /// <summary>
        /// Giving up is allowed in any idle zone — it is the "I am in a bad spot" exit — but unlike
        /// collecting it forfeits the bank, so it is never a free reroll.
        /// </summary>
        public static bool CanGiveUp(RunPhase phase) => phase == RunPhase.Idle;

        public static bool CanSpin(RunPhase phase) => phase == RunPhase.Idle;
    }
}
