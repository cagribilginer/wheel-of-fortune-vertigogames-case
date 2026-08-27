using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// The only state that accepts player input.
    /// <para>
    /// Note that it asks <see cref="RunModel"/> (and through it CashOutPolicy) whether each action is
    /// legal rather than deciding for itself, so the button states and the guard clauses can never drift
    /// apart — they are the same rule read twice.
    /// </para>
    /// </summary>
    public sealed class IdleState : GameStateBase
    {
        public IdleState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.Phase = RunPhase.Idle;
            Context.Presentation.SetInputState(
                canSpin: Context.Run.CanSpin,
                canLeave: Context.Run.CanLeave,
                canGiveUp: Context.Run.CanGiveUp);
        }

        public override void Exit() =>
            Context.Presentation.SetInputState(canSpin: false, canLeave: false, canGiveUp: false);

        public override void OnSpinRequested()
        {
            if (!Context.Run.CanSpin) return;
            Machine.Change<SpinningState>();
        }

        public override void OnLeaveRequested()
        {
            if (!Context.Run.CanLeave) return;
            Machine.Change<CashOutState>();
        }

        public override void OnGiveUpRequested()
        {
            if (!Context.Run.CanGiveUp) return;
            Machine.Change<GiveUpConfirmState>();
        }

        /// <summary>
        /// The single EXIT button's action: walk away with the haul when that is legal, otherwise fall back
        /// to the (bank-forfeiting) give-up confirm — the same two rules <see cref="OnLeaveRequested"/> and
        /// <see cref="OnGiveUpRequested"/> already enforce, just resolved in one place instead of needing two
        /// separate buttons to expose them.
        /// </summary>
        public override void OnExitRequested()
        {
            if (Context.Run.CanLeave) { Machine.Change<CashOutState>(); return; }
            if (Context.Run.CanGiveUp) Machine.Change<GiveUpConfirmState>();
        }
    }
}
