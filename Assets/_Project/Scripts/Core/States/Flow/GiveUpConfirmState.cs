namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Abandoning a run from a risky zone. Confirmed, it forfeits the haul and restarts from zone one —
    /// deliberately costly, so it is an escape hatch rather than a free reroll.
    /// </summary>
    public sealed class GiveUpConfirmState : GameStateBase
    {
        public GiveUpConfirmState(GameContext context) : base(context) { }

        public override void Enter() =>
            Context.Presentation.ShowGiveUpConfirm(Context.Run.Bank.DistinctRewardCount);

        public override void OnConfirmed()
        {
            Context.Presentation.HideGiveUpConfirm();
            Context.Run.GiveUp();
            Context.Run.ResetRun();
            Machine.Change<ZoneSetupState>();
        }

        public override void OnCancelled()
        {
            Context.Presentation.HideGiveUpConfirm();
            Machine.Change<IdleState>();
        }
    }
}
