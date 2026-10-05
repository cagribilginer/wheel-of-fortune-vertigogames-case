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
        [SerializeField] private Image _imagePopupCollectBackdrop;
        [SerializeField] private RectTransform _transformPopupCollectAnim;
        [SerializeField] private TextMeshProUGUI _textPopupCollectZoneValue;
        [SerializeField] private RectTransform _rowPopupCollectCurrency;
        [SerializeField] private RectTransform _contentPopupCollectList;
        [SerializeField] private Button _buttonPopupCollectConfirm;
        [SerializeField] private Button _buttonPopupCollectCancel;

        public RectTransform Content
        {
            get { return _contentPopupCollectList; }
        }

        /// <summary>Where the presenter pools the wallet's currency rows.</summary>
        public RectTransform CurrencyContent
        {
            get { return _rowPopupCollectCurrency; }
        }

        private Action _onClaimFinished;

        public event Action ConfirmClicked;
        public event Action CancelClicked;

        #region Wiring
        protected override void CacheReferences()
        {
            Bind(ref _imagePopupCollectBackdrop, "ui_image_popup_collect_backdrop");
            Bind(ref _transformPopupCollectAnim, "ui_transform_popup_collect_anim");
            Bind(ref _textPopupCollectZoneValue, "ui_text_popup_collect_zone_value");
            Bind(ref _rowPopupCollectCurrency, "ui_row_popup_collect_currency");
            Bind(ref _contentPopupCollectList, "ui_content_popup_collect_list");
            Bind(ref _buttonPopupCollectConfirm, "ui_button_popup_collect_confirm");
            Bind(ref _buttonPopupCollectCancel, "ui_button_popup_collect_cancel");
        }

        private void OnEnable()
        {
            _buttonPopupCollectConfirm.onClick.AddListener(RaiseConfirm);
            _buttonPopupCollectCancel.onClick.AddListener(RaiseCancel);
        }

        private void OnDisable()
        {
            _buttonPopupCollectConfirm.onClick.RemoveListener(RaiseConfirm);
            _buttonPopupCollectCancel.onClick.RemoveListener(RaiseCancel);
        }

        private void RaiseConfirm()
        {
            if (ConfirmClicked != null) ConfirmClicked();
        }
        private void RaiseCancel()
        {
            if (CancelClicked != null) CancelClicked();
        }
        #endregion

        #region Presentation
        public void Show(int zonesCleared)
        {
            _textPopupCollectZoneValue.SetText("Cleared {0} zones", zonesCleared);

            // A fresh summary is fully interactive again.
            _buttonPopupCollectConfirm.interactable = true;
            _buttonPopupCollectCancel.interactable = true;

            PlayOpen(_imagePopupCollectBackdrop, _transformPopupCollectAnim);
        }

        /// <summary>
        /// The claim celebration: locks the buttons and punches the card, then calls
        /// <paramref name="onComplete"/> (the run resets there) and closes. The presenter counts the balances up.
        /// </summary>
        public void PlayClaim(Action onComplete)
        {
            _buttonPopupCollectConfirm.interactable = false;
            _buttonPopupCollectCancel.interactable = false;

            _transformPopupCollectAnim.DOKill();
            _transformPopupCollectAnim.localScale = Vector3.one;
            _transformPopupCollectAnim.DOPunchScale(Vector3.one * Juice.CardPunchScale, Juice.CardPunchDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            _onClaimFinished = onComplete;
            DOVirtual.DelayedCall(Juice.ClaimHoldDuration, OnClaimHoldElapsed).SetLink(gameObject);
        }

        private void OnClaimHoldElapsed()
        {
            if (_onClaimFinished != null) _onClaimFinished();
            Hide();
        }

        public void Hide()
        {
            PlayClose(_imagePopupCollectBackdrop, _transformPopupCollectAnim);
        }
        #endregion
    }
}
