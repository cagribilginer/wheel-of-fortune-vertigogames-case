using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// COLLECT and GIVE UP. Both buttons are found by <see cref="UIViewBase.Bind{T}"/> and wired with
    /// <c>AddListener</c> here — never an Inspector OnClick binding, which the hygiene validator treats as
    /// a hard error precisely because it is invisible in a diff and in a code review.
    /// <para>
    /// Interactable state is set by the presenter from <c>CashOutPolicy</c>; this view never decides
    /// legality itself, only reflects it.
    /// </para>
    /// </summary>
    public sealed class ActionBarView : UIViewBase
    {
        [SerializeField] private Button _ui_button_action_collect;
        [SerializeField] private TextMeshProUGUI _ui_text_action_collect_value;
        [SerializeField] private Button _ui_button_action_giveup;
        [SerializeField] private TextMeshProUGUI _ui_text_action_giveup_value;

        public event Action CollectClicked;
        public event Action GiveUpClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_button_action_collect, "ui_button_action_collect");
            Bind(ref _ui_text_action_collect_value, "ui_text_action_collect_value");
            Bind(ref _ui_button_action_giveup, "ui_button_action_giveup");
            Bind(ref _ui_text_action_giveup_value, "ui_text_action_giveup_value");
        }

        private void OnEnable()
        {
            _ui_button_action_collect.onClick.AddListener(RaiseCollect);
            _ui_button_action_giveup.onClick.AddListener(RaiseGiveUp);
        }

        private void OnDisable()
        {
            _ui_button_action_collect.onClick.RemoveListener(RaiseCollect);
            _ui_button_action_giveup.onClick.RemoveListener(RaiseGiveUp);
        }

        private void RaiseCollect() => CollectClicked?.Invoke();
        private void RaiseGiveUp() => GiveUpClicked?.Invoke();

        public void SetCollectInteractable(bool interactable) => _ui_button_action_collect.interactable = interactable;

        public void SetGiveUpInteractable(bool interactable) => _ui_button_action_giveup.interactable = interactable;
    }
}
