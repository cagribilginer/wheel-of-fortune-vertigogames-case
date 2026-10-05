using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// The only state that accepts player input. It asks the run whether each action is legal instead of
    /// deciding itself, so the buttons and the guards read the same rule.
    /// </summary>
    public sealed class IdleState : GameStateBase
    {
        public IdleState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Input.SetInputState(new InputState(canSpin: true, canLeave: Context.Run.CanLeave));
        }

        public override void Exit()
        {
            Context.Input.SetInputState(InputState.Locked);
        }

        public override void OnSpinRequested()
        {
            Machine.Change<SpinningState>();
        }

        /// <summary>
        /// The single EXIT button's action: walk away with the haul. Legal only on a safe or super zone with
        /// something banked, which <see cref="RunModel.CanLeave"/> checks.
        /// </summary>
        public override void OnExitRequested()
        {
            if (Context.Run.CanLeave) Machine.Change<CashOutState>();
        }
    }
}
