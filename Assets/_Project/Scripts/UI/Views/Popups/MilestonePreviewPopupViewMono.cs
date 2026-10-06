using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The teaser opened by a top-right milestone badge: preview cards and a one-line description of the zone tier.
    /// Purely informational, so <c>MilestonePreviewPresenter</c> drives it straight off the badge clicks.
    /// </summary>
    public sealed class MilestonePreviewPopupViewMono : PopupViewBaseMono
    {
        [SerializeField] private Image _imagePopupMilestoneBackdrop;
        [SerializeField] private Button _buttonPopupMilestoneBackdrop;
        [SerializeField] private RectTransform _transformPopupMilestoneAnim;
        [SerializeField] private TextMeshProUGUI _textPopupMilestoneTitleValue;
        [SerializeField] private TextMeshProUGUI _textPopupMilestoneDescValue;
        [SerializeField] private RectTransform _rowPopupMilestoneSafe;
        [SerializeField] private RectTransform _rowPopupMilestoneSuper;
        [SerializeField] private Button _buttonPopupMilestoneClose;

        public event Action CloseClicked;

        protected override void CacheReferences()
        {
            Bind(ref _imagePopupMilestoneBackdrop, "ui_image_popup_milestone_backdrop");
            Bind(ref _buttonPopupMilestoneBackdrop, "ui_image_popup_milestone_backdrop");
            Bind(ref _transformPopupMilestoneAnim, "ui_transform_popup_milestone_anim");
            Bind(ref _textPopupMilestoneTitleValue, "ui_text_popup_milestone_title_value");
            Bind(ref _textPopupMilestoneDescValue, "ui_text_popup_milestone_desc_value");
            Bind(ref _rowPopupMilestoneSafe, "ui_row_popup_milestone_safe");
            Bind(ref _rowPopupMilestoneSuper, "ui_row_popup_milestone_super");
            Bind(ref _buttonPopupMilestoneClose, "ui_button_popup_milestone_close");
        }

        private void OnEnable()
        {
            _buttonPopupMilestoneBackdrop.onClick.AddListener(RaiseClose);
            _buttonPopupMilestoneClose.onClick.AddListener(RaiseClose);
        }

        private void OnDisable()
        {
            _buttonPopupMilestoneBackdrop.onClick.RemoveListener(RaiseClose);
            _buttonPopupMilestoneClose.onClick.RemoveListener(RaiseClose);
        }

        private void RaiseClose()
        {
            if (CloseClicked != null) CloseClicked();
        }

        public void Show(bool isSuper)
        {
            _rowPopupMilestoneSafe.gameObject.SetActive(!isSuper);
            _rowPopupMilestoneSuper.gameObject.SetActive(isSuper);

            _textPopupMilestoneTitleValue.text = isSuper ? ViewText.SUPER_ZONE_TITLE : ViewText.SAFE_ZONE_TITLE;
            _textPopupMilestoneDescValue.text = isSuper ? ViewText.SUPER_ZONE_DESCRIPTION : ViewText.SAFE_ZONE_DESCRIPTION;

            PlayOpen(_imagePopupMilestoneBackdrop, _transformPopupMilestoneAnim);
        }

        public void Hide()
        {
            PlayClose(_imagePopupMilestoneBackdrop, _transformPopupMilestoneAnim);
        }
    }
}
