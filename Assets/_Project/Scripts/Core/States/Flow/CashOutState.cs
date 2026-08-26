using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Walking away with the haul. Banked gold converts to the persistent wallet here, which is the only
    /// route by which the wallet ever grows.
    /// <para>
    /// The bank is deliberately not cleared until the player dismisses the summary, so the popup can list
    /// what they actually won.
    /// </para>
    /// </summary>
    public sealed class CashOutState : GameStateBase
    {
        public CashOutState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.Phase = RunPhase.CashOut;
            Context.Run.CashOut();

            Context.Presentation.ShowCashOut(Context.Run.Bank.Entries, Context.Run.CurrentZone);
        }

        public override void OnConfirmed()
        {
            Context.Presentation.HideCashOut();
            Context.Run.ResetRun();
            Machine.Change<ZoneSetupState>();
        }
    }
}
