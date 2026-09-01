using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Walking away with the haul. The summary opens first and commits nothing: the player can still
    /// cancel back to the wheel and keep spinning. Only on confirm does banked gold convert to the
    /// persistent wallet — the one route by which the wallet ever grows — and the run reset.
    /// </summary>
    public sealed class CashOutState : GameStateBase
    {
        public CashOutState(GameContext context) : base(context) { }

        public override void Enter()
        {
            // Block the wheel while the summary is up, but leave the bank untouched so a cancel is a
            // genuine no-op.
            Context.Run.Phase = RunPhase.CashOut;

            // Zones are 1-indexed and CurrentZone is the one the player is standing on but has not
            // finished, so the number of *cleared* zones is one less.
            Context.Presentation.ShowCashOut(Context.Run.Bank.Entries, Context.Run.CurrentZone - 1);
        }

        public override void OnConfirmed()
        {
            // Credit the wallet now so the header counter can animate to the real new total during the
            // claim celebration. The run itself is not reset until that celebration finishes.
            Context.Run.CashOut();
            Context.Presentation.ClaimCashOut(() =>
            {
                Context.Run.ResetRun();
                Machine.Change<ZoneSetupState>();
            });
        }

        public override void OnCancelled()
        {
            Context.Presentation.HideCashOut();
            Context.Run.Phase = RunPhase.Idle;
            Machine.Change<IdleState>();
        }
    }
}
