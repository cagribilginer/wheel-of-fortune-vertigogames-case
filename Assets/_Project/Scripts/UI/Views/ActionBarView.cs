using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The single EXIT action. Bound by <see cref="UIViewBase.Bind{T}"/> and wired with <c>AddListener</c>
    /// here — never an Inspector OnClick binding, which the hygiene validator treats as a hard error
    /// precisely because it is invisible in a diff and in a code review.
    /// <para>
    /// Interactable state is set by the presenter from <c>CashOutPolicy</c>; this view never decides
    /// legality itself, only reflects it. Which of "cash out" or "give up" EXIT actually triggers is a
    /// state-machine decision (<c>IdleState.OnExitRequested</c>), not this view's concern either.
    /// </para>
    /// </summary>
    public sealed class ActionBarView : UIViewBase
    {
        [SerializeField] private Button _ui_button_action_exit;
        [SerializeField] private TextMeshProUGUI _ui_text_action_exit_value;

        public event Action ExitClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_button_action_exit, "ui_button_action_exit");
            Bind(ref _ui_text_action_exit_value, "ui_text_action_exit_value");
        }

        private void OnEnable() => _ui_button_action_exit.onClick.AddListener(RaiseExit);

        private void OnDisable() => _ui_button_action_exit.onClick.RemoveListener(RaiseExit);

        private void RaiseExit() => ExitClicked?.Invoke();

        public void SetExitInteractable(bool interactable) => _ui_button_action_exit.interactable = interactable;
    }
}
