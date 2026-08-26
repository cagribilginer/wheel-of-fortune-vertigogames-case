using Vertigo.Wheel.Core.Run;

namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>The reveal beat between the wheel stopping and the consequence landing.</summary>
    public sealed class ResolvingState : GameStateBase
    {
        public ResolvingState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.Phase = RunPhase.Resolving;

            Context.Presentation.PlayReveal(Context.PendingOutcome, () =>
            {
                if (Context.PendingOutcome.IsBomb) Machine.Change<BombHitState>();
                else Machine.Change<RewardGrantedState>();
            });
        }
    }
}
