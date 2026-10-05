using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Decides the outcome, then asks the wheel to animate to it, so the result exists before any rotation.
    /// No input is accepted here, so a second tap is ignored.
    /// </summary>
    public sealed class SpinningState : GameStateBase
    {
        public SpinningState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.PendingOutcome = Context.SpinService.Spin(Context.CurrentWheel);

            Context.Spin.PlaySpin(
                Context.PendingOutcome.SlotIndex,
                OnSpinStopped);
        }

        private void OnSpinStopped()
        {
            Machine.Change<ResolvingState>();
        }
    }
}
