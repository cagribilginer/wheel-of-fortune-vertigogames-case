using System;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The single EXIT action, wired with <c>AddListener</c>, never an Inspector OnClick. Interactable state comes
    /// from the presenter (CashOutPolicy); what EXIT triggers is a state-machine decision.
    /// </summary>
    public sealed class ActionBarView : UIViewBase
    {
        [SerializeField] private Button _ui_button_action_exit;

        public event Action ExitClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_button_action_exit, "ui_button_action_exit");
        }

        private void OnEnable()
        {
            _ui_button_action_exit.onClick.AddListener(RaiseExit);
        }

        private void OnDisable()
        {
            _ui_button_action_exit.onClick.RemoveListener(RaiseExit);
        }

        private void RaiseExit()
        {
            if (ExitClicked != null) ExitClicked();
        }

        public void SetExitInteractable(bool interactable)
        {
            _ui_button_action_exit.interactable = interactable;
        }
    }
}
