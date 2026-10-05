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
        [SerializeField] private Button _buttonActionExit;

        public event Action ExitClicked;

        protected override void CacheReferences()
        {
            Bind(ref _buttonActionExit, "ui_button_action_exit");
        }

        private void OnEnable()
        {
            _buttonActionExit.onClick.AddListener(RaiseExit);
        }

        private void OnDisable()
        {
            _buttonActionExit.onClick.RemoveListener(RaiseExit);
        }

        private void RaiseExit()
        {
            if (ExitClicked != null) ExitClicked();
        }

        public void SetExitInteractable(bool interactable)
        {
            _buttonActionExit.interactable = interactable;
        }
    }
}
