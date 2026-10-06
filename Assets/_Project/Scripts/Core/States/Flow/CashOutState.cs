using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Walking away with the haul. The summary opens first and commits nothing: the player can still
    /// cancel back to the wheel and keep spinning. Only on confirm does banked gold convert to the
    /// persistent wallet — the one route by which the wallet ever grows — and a fresh run boots. The claim
    /// celebration keeps this state current for its whole duration, so input arriving in that window must neither
    /// credit the wallet a second time nor cancel a claim that already paid out.
    /// </summary>
    public sealed class CashOutState : GameStateBase, IConfirmInputHandler, ICancelInputHandler
    {
        private bool _isClaiming;

        public CashOutState(GameContext context) : base(context) { }

        public override void Enter()
        {
            _isClaiming = false;

            Context.CashOut.ShowCashOut(
                Context.Run.WalletGains, Context.Run.CurrentZone - 1, Context.Run.Balances);
        }

        public void OnConfirmed()
        {
            if (_isClaiming) return;
            _isClaiming = true;

            Context.Run.CashOut();
            Context.CashOut.ClaimCashOut(Context.Run.Balances, OnClaimFinished);
        }

        private void OnClaimFinished()
        {
            Machine.Change<BootState>();
        }

        public void OnCancelled()
        {
            if (_isClaiming) return;

            Context.CashOut.HideCashOut();
            Machine.Change<IdleState>();
        }
    }
}
