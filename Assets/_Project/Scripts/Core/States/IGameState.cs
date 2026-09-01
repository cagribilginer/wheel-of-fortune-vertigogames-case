namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// One mode of the game. Input legality, button state and popup visibility all differ per mode, which
    /// is what makes this a genuine state machine rather than a bag of booleans that grows quadratically.
    /// </summary>
    public interface IGameState
    {
        void Enter();
        void Exit();

        void OnSpinRequested();
        void OnLeaveRequested();
        void OnGiveUpRequested();
        void OnExitRequested();
        void OnConfirmed();
        void OnCancelled();
        void OnRestartRequested();
        void OnContinueRequested();
        void OnAdContinueRequested();
    }
}
