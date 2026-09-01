namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Offers the ways out of a bomb: give up (forfeit the haul, restart from zone one), revive with gold,
    /// or revive by watching an ad. Either revive pours the snapshotted haul back into the bank and resumes
    /// the same zone.
    /// <para>
    /// The gold revive is offered whenever the player can afford its (doubling) price — no per-run cap. The
    /// ad revive is the one free escape and is capped per run.
    /// </para>
    /// </summary>
    public sealed class GameOverState : GameStateBase
    {
        public GameOverState(GameContext context) : base(context) { }

        public override void Enter()
        {
            int zoneReached = Context.Run.CurrentZone;
            int goldUsed = Context.Run.GoldRevivesUsedThisRun;
            int adUsed = Context.Run.AdRevivesUsedThisRun;

            Context.Presentation.ShowGameOver(
                zoneReached,
                Context.Run.LostHaul,
                Context.Run.WalletBalance,
                Context.ContinueService.IsGoldReviveOffered(zoneReached, goldUsed),
                Context.ContinueService.CostFor(zoneReached, goldUsed),
                Context.ContinueService.IsAdReviveOffered(adUsed));
        }

        // "Give up" on the bomb screen forfeits the haul and drops back to zone one — mechanically a restart.
        // The model is reset before the screen closes so the bank the screen refreshes on its way out is
        // already the empty one the player restarts with.
        public override void OnRestartRequested()
        {
            Context.Run.ResetRun();
            Context.Presentation.HideGameOver();
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
            Context.Presentation.HideGameOver();
            Machine.Change<IdleState>();
        }
    }
}
