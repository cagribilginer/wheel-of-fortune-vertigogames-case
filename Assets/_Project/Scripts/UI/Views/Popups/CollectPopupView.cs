using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The cash-out confirmation, listing the haul in pooled <see cref="BankEntryView"/> cells. Nothing is committed while
    /// it is open: cancel returns to the wheel with the haul intact, confirm runs <see cref="PlayClaim"/> and then the
    /// state machine resets the run.
    /// </summary>
    public sealed class CollectPopupView : PopupViewBase
    {
        [SerializeField] private Image _ui_image_popup_collect_backdrop;
        [SerializeField] private RectTransform _ui_transform_popup_collect_anim;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_zone_value;
        [SerializeField] private RectTransform _ui_row_popup_collect_currency;
        [SerializeField] private RectTransform _ui_content_popup_collect_list;
        [SerializeField] private Button _ui_button_popup_collect_confirm;
        [SerializeField] private Button _ui_button_popup_collect_cancel;

        public RectTransform Content
        {
            get { return _ui_content_popup_collect_list; }
        }

        /// <summary>Where the presenter pools the wallet's currency rows.</summary>
        public RectTransform CurrencyContent
        {
            get { return _ui_row_popup_collect_currency; }
        }

        public event Action ConfirmClicked;
        public event Action CancelClicked;

        #region Wiring
        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_collect_backdrop, "ui_image_popup_collect_backdrop");
            Bind(ref _ui_transform_popup_collect_anim, "ui_transform_popup_collect_anim");
            Bind(ref _ui_text_popup_collect_zone_value, "ui_text_popup_collect_zone_value");
            Bind(ref _ui_row_popup_collect_currency, "ui_row_popup_collect_currency");
            Bind(ref _ui_content_popup_collect_list, "ui_content_popup_collect_list");
            Bind(ref _ui_button_popup_collect_confirm, "ui_button_popup_collect_confirm");
            Bind(ref _ui_button_popup_collect_cancel, "ui_button_popup_collect_cancel");
        }

        private void OnEnable()
        {
            _ui_button_popup_collect_confirm.onClick.AddListener(RaiseConfirm);
            _ui_button_popup_collect_cancel.onClick.AddListener(RaiseCancel);
        }

        private void OnDisable()
        {
            _ui_button_popup_collect_confirm.onClick.RemoveListener(RaiseConfirm);
            _ui_button_popup_collect_cancel.onClick.RemoveListener(RaiseCancel);
        }

        private void RaiseConfirm()
        {
            ConfirmClicked?.Invoke();
        }
        private void RaiseCancel()
        {
            CancelClicked?.Invoke();
        }
        #endregion

        #region Presentation
        public void Show(int zonesCleared)
        {
            _ui_text_popup_collect_zone_value.SetText("Cleared {0} zones", zonesCleared);

            // A fresh summary is fully interactive again.
            _ui_button_popup_collect_confirm.interactable = true;
            _ui_button_popup_collect_cancel.interactable = true;

            PlayOpen(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
        }

        /// <summary>
        /// The claim celebration: locks the buttons and punches the card, then calls
        /// <paramref name="onComplete"/> (the run resets there) and closes. The presenter counts the balances up.
        /// </summary>
        public void PlayClaim(Action onComplete)
        {
            _ui_button_popup_collect_confirm.interactable = false;
            _ui_button_popup_collect_cancel.interactable = false;

            _ui_transform_popup_collect_anim.DOKill();
            _ui_transform_popup_collect_anim.localScale = Vector3.one;
            _ui_transform_popup_collect_anim.DOPunchScale(Vector3.one * Juice.CardPunchScale, Juice.CardPunchDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            DOVirtual.DelayedCall(Juice.ClaimHoldDuration, () =>
            {
                onComplete?.Invoke();
                Hide();
            }).SetLink(gameObject);
        }

        public void Hide()
        {
            PlayClose(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
        }
        #endregion
    }
}
