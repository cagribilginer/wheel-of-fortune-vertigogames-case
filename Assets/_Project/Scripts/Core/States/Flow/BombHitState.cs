namespace Vertigo.Wheel.Core.States.Flow
{
    /// <summary>Clears the haul and plays the consequence before the game-over screen appears.</summary>
    public sealed class BombHitState : GameStateBase
    {
        public BombHitState(GameContext context) : base(context) { }

        public override void Enter()
        {
            Context.Run.Detonate();
            Context.Presentation.PlayBomb(() => Machine.Change<GameOverState>());
        }
    }
}
