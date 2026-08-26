using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>
    /// Decides the outcome, then asks the wheel to animate to it.
    /// <para>
    /// The ordering is the point: the result exists before a single degree of rotation. The animation
    /// renders a committed number rather than the number being read back off wherever a float angle
    /// happened to settle.
    /// </para>
    /// <para>No input is accepted here, so a second tap on spin is simply ignored.</para>
    /// </summary>
    public sealed class SpinningState : GameStateBase
    {
        public SpinningState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.Phase = RunPhase.Spinning;
            Context.PendingOutcome = Context.SpinService.Spin(Context.CurrentWheel);

            Context.Presentation.PlaySpin(
                Context.PendingOutcome.SlotIndex,
                () => Machine.Change<ResolvingState>());
        }
    }
}
