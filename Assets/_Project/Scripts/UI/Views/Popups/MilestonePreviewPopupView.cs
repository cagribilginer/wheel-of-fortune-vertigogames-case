using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The teaser that opens when the player taps a top-right milestone badge: a row of preview cards on a
    /// dark overlay, a title, and a one-line description of what that zone tier is worth. Purely
    /// informational — it changes no game state — so it is driven by <c>MilestonePreviewPresenter</c>
    /// straight off the badge clicks, not through <c>IWheelPresentation</c>.
    /// <para>Tapping the backdrop or the corner X closes it.</para>
    /// </summary>
    public sealed class MilestonePreviewPopupView : PopupViewBase
    {
        [SerializeField] private Image _ui_image_popup_milestone_backdrop;
        [SerializeField] private Button _ui_button_popup_milestone_backdrop;
        [SerializeField] private RectTransform _ui_transform_popup_milestone_anim;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_milestone_title_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_milestone_desc_value;
        [SerializeField] private RectTransform _ui_row_popup_milestone_safe;
        [SerializeField] private RectTransform _ui_row_popup_milestone_super;
        [SerializeField] private Button _ui_button_popup_milestone_close;

        private const string SafeDescription = "Win special rewards in bomb-free Safe Zones!";
        private const string SuperDescription = "Win super rewards in bomb-free Super Zones!";

        public event Action CloseClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_milestone_backdrop, "ui_image_popup_milestone_backdrop");
            Bind(ref _ui_button_popup_milestone_backdrop, "ui_image_popup_milestone_backdrop");
            Bind(ref _ui_transform_popup_milestone_anim, "ui_transform_popup_milestone_anim");
            Bind(ref _ui_text_popup_milestone_title_value, "ui_text_popup_milestone_title_value");
            Bind(ref _ui_text_popup_milestone_desc_value, "ui_text_popup_milestone_desc_value");
            Bind(ref _ui_row_popup_milestone_safe, "ui_row_popup_milestone_safe");
            Bind(ref _ui_row_popup_milestone_super, "ui_row_popup_milestone_super");
            Bind(ref _ui_button_popup_milestone_close, "ui_button_popup_milestone_close");
        }

        private void OnEnable()
        {
            _ui_button_popup_milestone_backdrop.onClick.AddListener(RaiseClose);
            _ui_button_popup_milestone_close.onClick.AddListener(RaiseClose);
        }

        private void OnDisable()
        {
            _ui_button_popup_milestone_backdrop.onClick.RemoveListener(RaiseClose);
            _ui_button_popup_milestone_close.onClick.RemoveListener(RaiseClose);
        }

        private void RaiseClose() => CloseClicked?.Invoke();

        public void Show(bool isSuper)
        {
            _ui_row_popup_milestone_safe.gameObject.SetActive(!isSuper);
            _ui_row_popup_milestone_super.gameObject.SetActive(isSuper);

            _ui_text_popup_milestone_title_value.text = isSuper ? "SUPER ZONE" : "SAFE ZONE";
            _ui_text_popup_milestone_desc_value.text = isSuper ? SuperDescription : SafeDescription;

            PlayOpen(_ui_image_popup_milestone_backdrop, _ui_transform_popup_milestone_anim);
        }

        public void Hide() => PlayClose(_ui_image_popup_milestone_backdrop, _ui_transform_popup_milestone_anim);
    }
}
