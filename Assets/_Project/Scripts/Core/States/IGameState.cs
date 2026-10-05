namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// One mode of the game. Input legality, button state and popup visibility all differ per mode, which
    /// is what makes this a genuine state machine rather than a bag of booleans that grows quadratically.
    /// The lifecycle is all a state must have; the inputs it accepts are the <c>I…InputHandler</c>
    /// interfaces it chooses to implement.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Exit();
    }
}
