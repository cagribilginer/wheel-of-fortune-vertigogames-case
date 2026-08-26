namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Offers the two ways out of a bomb: pay to carry on from the same zone, or restart from zone one.
    /// <para>
    /// Continue is offered only when it is both affordable and unused this run; the wallet check happens
    /// here so an unaffordable button is never shown at all rather than shown and then refused.
    /// </para>
    /// </summary>
    public sealed class GameOverState : GameStateBase
    {
        public GameOverState(GameContext context) : base(context) { }

        public override void Enter()
        {
            int zoneReached = Context.Run.CurrentZone;
            bool offered = Context.ContinueService.IsOffered(zoneReached, Context.Run.ContinuesUsedThisRun);

            Context.Presentation.ShowGameOver(zoneReached, offered, Context.ContinueService.CostFor(zoneReached));
        }

        public override void OnRestartRequested()
        {
            Context.Presentation.HideGameOver();
            Context.Run.ResetRun();
            Machine.Change<ZoneSetupState>();
        }

        public override void OnContinueRequested()
        {
            int zoneReached = Context.Run.CurrentZone;

            // The purchase is the gate. If it fails the popup simply stays up.
            if (!Context.ContinueService.TryPurchase(zoneReached, Context.Run.ContinuesUsedThisRun)) return;

            Context.Presentation.HideGameOver();
            Context.Run.ApplyContinue();
            Machine.Change<IdleState>();
        }
    }
}
