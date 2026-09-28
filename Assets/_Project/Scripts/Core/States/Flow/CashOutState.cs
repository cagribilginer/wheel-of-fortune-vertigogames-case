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
        // The claim celebration keeps this state current for its whole duration; input arriving in that
        // window must neither credit the wallet a second time nor cancel a claim that already paid out.
        private bool _claiming;

        public CashOutState(GameContext context) : base(context) { }

        public override void Enter()
        {
            _claiming = false;

            // Block the wheel while the summary is up, but leave the bank untouched so a cancel is a
            // genuine no-op.
            Context.Run.Phase = RunPhase.CashOut;

            // CurrentZone is the one being stood on, so cleared zones are one fewer. The wallet shows as before the claim.
            Context.CashOut.ShowCashOut(
                Context.Run.Bank.Entries, Context.Run.CurrentZone - 1, Context.Run.Balances);
        }

        public override void OnConfirmed()
        {
            if (_claiming) return;
            _claiming = true;

            // Credit the wallet, then read the new balances back for the count-up. The run resets after the celebration.
            Context.Run.CashOut();
            Context.CashOut.ClaimCashOut(Context.Run.Balances, () =>
            {
                Context.Run.ResetRun();
                Machine.Change<ZoneSetupState>();
            });
        }

        public override void OnCancelled()
        {
            if (_claiming) return;

            Context.CashOut.HideCashOut();
            Context.Run.Phase = RunPhase.Idle;
            Machine.Change<IdleState>();
        }
    }
}
