namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// One mode of the game. Only the lifecycle is required; the inputs a state accepts are the
    /// <c>I…InputHandler</c> interfaces it chooses to implement.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Exit();
    }
}
