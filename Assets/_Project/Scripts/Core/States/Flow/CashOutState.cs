using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Walking away with the haul. The summary commits nothing until the player confirms, which credits the
    /// wallet and boots a fresh run. The claim keeps this state current, so input during it is ignored.
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
