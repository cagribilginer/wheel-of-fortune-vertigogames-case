using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>COLLECT / GIVE UP: forwards clicks to the state machine and mirrors input legality.</summary>
    public sealed class ActionBarPresenter
    {
        private readonly ActionBarView _view;

        public ActionBarPresenter(ActionBarView view) => _view = view;

        public void WireInput(GameStateMachine machine)
        {
            _view.CollectClicked += machine.RequestLeave;
            _view.GiveUpClicked += machine.RequestGiveUp;
        }

        public void SetInputState(bool canLeave, bool canGiveUp)
        {
            _view.SetCollectInteractable(canLeave);
            _view.SetGiveUpInteractable(canGiveUp);
        }
    }
}
