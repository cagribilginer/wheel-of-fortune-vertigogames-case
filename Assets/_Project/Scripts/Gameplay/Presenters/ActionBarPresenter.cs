using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>EXIT: forwards the click to the state machine and mirrors input legality.</summary>
    public sealed class ActionBarPresenter
    {
        private readonly ActionBarView _view;

        public ActionBarPresenter(ActionBarView view) => _view = view;

        public void WireInput(GameStateMachine machine) => _view.ExitClicked += machine.RequestExit;

        /// <summary>EXIT is interactable whenever either of the two legal exits it can trigger is available.</summary>
        public void SetInputState(bool canLeave, bool canGiveUp) => _view.SetExitInteractable(canLeave || canGiveUp);
    }
}
