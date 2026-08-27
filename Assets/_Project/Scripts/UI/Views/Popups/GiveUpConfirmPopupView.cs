using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>Confirms abandoning a run before the haul is actually forfeited.</summary>
    public sealed class GiveUpConfirmPopupView : UIViewBase
    {
        [SerializeField] private Image _ui_image_popup_confirm_giveup_backdrop;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_confirm_giveup_body_value;
        [SerializeField] private Button _ui_button_popup_confirm_giveup_yes;
        [SerializeField] private Button _ui_button_popup_confirm_giveup_no;

        public event Action ConfirmClicked;
        public event Action CancelClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_confirm_giveup_backdrop, "ui_image_popup_confirm_giveup_backdrop");
            Bind(ref _ui_text_popup_confirm_giveup_body_value, "ui_text_popup_confirm_giveup_body_value");
            Bind(ref _ui_button_popup_confirm_giveup_yes, "ui_button_popup_confirm_giveup_yes");
            Bind(ref _ui_button_popup_confirm_giveup_no, "ui_button_popup_confirm_giveup_no");
        }

        private void OnEnable()
        {
            _ui_button_popup_confirm_giveup_yes.onClick.AddListener(RaiseConfirm);
            _ui_button_popup_confirm_giveup_no.onClick.AddListener(RaiseCancel);
        }

        private void OnDisable()
        {
            _ui_button_popup_confirm_giveup_yes.onClick.RemoveListener(RaiseConfirm);
            _ui_button_popup_confirm_giveup_no.onClick.RemoveListener(RaiseCancel);
        }

        private void RaiseConfirm() => ConfirmClicked?.Invoke();
        private void RaiseCancel() => CancelClicked?.Invoke();

        public void Show(int rewardsAtStake)
        {
            // SetText's zero-alloc overloads only take float args; the plural suffix is a string, so this
            // one substitution has to go through the regular text setter instead.
            _ui_text_popup_confirm_giveup_body_value.text =
                $"You will lose {rewardsAtStake} reward{(rewardsAtStake == 1 ? string.Empty : "s")}.";

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
