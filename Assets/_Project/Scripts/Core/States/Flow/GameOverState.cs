namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// The ways out of a bomb: give up and boot a fresh run, or revive with gold or an ad, which restores the
    /// snapshotted haul on the same zone. Gold has no per-run cap; the ad revive does.
    /// </summary>
    public sealed class GameOverState : GameStateBase, IRestartInputHandler, IContinueInputHandler, IAdContinueInputHandler
    {
        public GameOverState(GameContext context) : base(context) { }

        public override void Enter()
        {
            int zoneReached = Context.Run.CurrentZone;
            int goldUsed = Context.Run.GoldRevivesUsedThisRun;
            int adUsed = Context.Run.AdRevivesUsedThisRun;

            Context.GameOver.ShowGameOver(new GameOverSummary(
                zoneReached,
                Context.Run.LostHaul,
                Context.Run.Balances,
                Context.ContinueService.IsGoldReviveOffered(zoneReached, goldUsed),
                Context.ContinueService.CostFor(zoneReached, goldUsed),
                Context.ContinueService.IsAdReviveOffered(adUsed)));
        }

        public void OnRestartRequested()
        {
            Context.GameOver.HideGameOver();
            Machine.Change<BootState>();
        }

        public void OnContinueRequested()
        {
            int zoneReached = Context.Run.CurrentZone;

            if (!Context.ContinueService.TryPurchase(zoneReached, Context.Run.GoldRevivesUsedThisRun)) return;

            Context.Run.ApplyGoldRevive();
            Revive();
        }

        public void OnAdContinueRequested()
        {
            if (!Context.ContinueService.IsAdReviveOffered(Context.Run.AdRevivesUsedThisRun)) return;

            Context.Run.ApplyAdRevive();
            Revive();
        }

        private void Revive()
        {
            Context.GameOver.HideGameOver();
            Machine.Change<IdleState>();
        }
    }
}
