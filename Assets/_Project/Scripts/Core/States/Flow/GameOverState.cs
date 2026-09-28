namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// The ways out of a bomb: give up and restart at zone one, or revive with gold or an ad, which restores
    /// the snapshotted haul on the same zone. Gold has no per-run cap; the ad revive does.
    /// </summary>
    public sealed class GameOverState : GameStateBase
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

        // Give up is a restart. The model resets before the screen closes, so the bank it refreshes is already empty.
        public override void OnRestartRequested()
        {
            Context.Run.ResetRun();
            Context.GameOver.HideGameOver();
            Machine.Change<ZoneSetupState>();
        }

        public override void OnContinueRequested()
        {
            int zoneReached = Context.Run.CurrentZone;

            // The purchase is the gate. If it fails the popup simply stays up.
            if (!Context.ContinueService.TryPurchase(zoneReached, Context.Run.GoldRevivesUsedThisRun)) return;

            Context.Run.ApplyGoldRevive();
            Revive();
        }

        public override void OnAdContinueRequested()
        {
            // No wallet debit — watching the video is the price. Capped per run.
            if (!Context.ContinueService.IsAdReviveOffered(Context.Run.AdRevivesUsedThisRun)) return;

            Context.Run.ApplyAdRevive();
            Revive();
        }

        private void Revive()
        {
            // ApplyGold/AdRevive has already restored the haul, so the bank HideGameOver refreshes on its
            // way out shows the rewards the player just kept, not the empty bank the bomb left behind.
            Context.GameOver.HideGameOver();
            Machine.Change<IdleState>();
        }
    }
}
