using Vertigo.Wheel.Core.States.Flow;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Assembles the state machine. One place that knows the full set of states, shared by the composition
    /// root and by the tests, so the flow under test is the flow that ships.
    /// </summary>
    public static class GameFlow
    {
        public static GameStateMachine Build(GameContext context)
        {
            var machine = new GameStateMachine();
            context.Machine = machine;

            machine.Register(new BootState(context));
            machine.Register(new ZoneSetupState(context));
            machine.Register(new IdleState(context));
            machine.Register(new SpinningState(context));
            machine.Register(new ResolvingState(context));
            machine.Register(new RewardGrantedState(context));
            machine.Register(new BombHitState(context));
            machine.Register(new GameOverState(context));
            machine.Register(new CashOutState(context));
            machine.Register(new GiveUpConfirmState(context));

            return machine;
        }

        /// <summary>Enters the flow. Boot resets the run and falls through to the first zone.</summary>
        public static void Start(GameStateMachine machine) => machine.Change<BootState>();
    }
}
