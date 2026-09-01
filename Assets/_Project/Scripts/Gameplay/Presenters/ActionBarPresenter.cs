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

        /// <summary>
        /// EXIT now only ever cashes out, so it is interactable exactly when leaving is legal — an idle
        /// wheel with something banked. <paramref name="canGiveUp"/> is still passed by the flow but the
        /// give-up confirm is no longer wired to this button.
        /// </summary>
        public void SetInputState(bool canLeave, bool canGiveUp) => _view.SetExitInteractable(canLeave);
    }
}
