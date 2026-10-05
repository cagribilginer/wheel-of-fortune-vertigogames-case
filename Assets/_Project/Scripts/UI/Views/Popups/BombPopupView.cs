using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Core.States;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The bomb defeat / revive screen: the lost haul, the currency HUD and three buttons on a near-black backdrop
    /// with a breathing red vignette. An unavailable revive is shown disabled, not removed, so the row never reflows.
    /// </summary>
    public sealed class BombPopupView : PopupViewBase
    {
        [SerializeField] private Image _imagePopupBombBackdrop;
        [SerializeField] private Image _imagePopupBombVignette;
        [SerializeField] private RectTransform _transformPopupBombAnim;
        [SerializeField] private TextMeshProUGUI _textPopupBombZoneValue;
        [SerializeField] private RectTransform _rowPopupBombCurrency;
        [SerializeField] private RectTransform _contentPopupBombList;
        [SerializeField] private TextMeshProUGUI _textPopupBombEmptyValue;
        [SerializeField] private Button _buttonPopupBombGiveup;
        [SerializeField] private Button _buttonPopupBombContinue;
        [SerializeField] private TextMeshProUGUI _textPopupBombContinueValue;
        [SerializeField] private Button _buttonPopupBombAdvert;

        /// <summary>Where the presenter pools the lost-haul preview tiles.</summary>
        public RectTransform Content
        {
            get { return _contentPopupBombList; }
        }

        /// <summary>Where the presenter pools the wallet's currency rows.</summary>
        public RectTransform CurrencyContent
        {
            get { return _rowPopupBombCurrency; }
        }

        public event Action GiveUpClicked;
        public event Action ContinueClicked;
        public event Action AdContinueClicked;

        #region Wiring
        protected override void CacheReferences()
        {
            Bind(ref _imagePopupBombBackdrop, "ui_image_popup_bomb_backdrop");
            Bind(ref _imagePopupBombVignette, "ui_image_popup_bomb_vignette");
            Bind(ref _transformPopupBombAnim, "ui_transform_popup_bomb_anim");
            Bind(ref _textPopupBombZoneValue, "ui_text_popup_bomb_zone_value");
            Bind(ref _rowPopupBombCurrency, "ui_row_popup_bomb_currency");
            Bind(ref _contentPopupBombList, "ui_content_popup_bomb_list");
            Bind(ref _textPopupBombEmptyValue, "ui_text_popup_bomb_empty_value");
            Bind(ref _buttonPopupBombGiveup, "ui_button_popup_bomb_giveup");
            Bind(ref _buttonPopupBombContinue, "ui_button_popup_bomb_continue");
            Bind(ref _textPopupBombContinueValue, "ui_text_popup_bomb_continue_value");
            Bind(ref _buttonPopupBombAdvert, "ui_button_popup_bomb_advert");
        }

        private void OnEnable()
        {
            _buttonPopupBombGiveup.onClick.AddListener(RaiseGiveUp);
            _buttonPopupBombContinue.onClick.AddListener(RaiseContinue);
            _buttonPopupBombAdvert.onClick.AddListener(RaiseAdContinue);
        }

        private void OnDisable()
        {
            _buttonPopupBombGiveup.onClick.RemoveListener(RaiseGiveUp);
            _buttonPopupBombContinue.onClick.RemoveListener(RaiseContinue);
            _buttonPopupBombAdvert.onClick.RemoveListener(RaiseAdContinue);
        }

        private void RaiseGiveUp()
        {
            if (GiveUpClicked != null) GiveUpClicked();
        }
        private void RaiseContinue()
        {
            if (ContinueClicked != null) ContinueClicked();
        }
        private void RaiseAdContinue()
        {
            if (AdContinueClicked != null) AdContinueClicked();
        }
        #endregion

        #region Presentation
        public void Show(GameOverSummary summary)
        {
            _textPopupBombZoneValue.SetText("You reached Zone {0}", summary.ZoneReached);

            // SetText's zero-alloc formatter does not honour ":N0" (it prints the literal characters), so the
            // thousands separator has to come from the regular setter.
            _textPopupBombContinueValue.text = summary.GoldReviveCost.ToString("N0");

            _textPopupBombEmptyValue.gameObject.SetActive(summary.LostHaul.Count == 0);

            // Every button stays in the row; an unavailable revive is disabled, not hidden.
            _buttonPopupBombContinue.interactable = summary.IsGoldReviveOffered;
            _buttonPopupBombAdvert.interactable = summary.IsAdReviveOffered;

            PlayVignette();
            PlayOpen(_imagePopupBombBackdrop, _transformPopupBombAnim, Juice.BombBackdropAlpha);
        }

        public void Hide()
        {
            _imagePopupBombVignette.DOKill();
            _imagePopupBombVignette.DOFade(0f, Juice.PopupFadeDuration)
                .SetLink(_imagePopupBombVignette.gameObject, LinkBehaviour.KillOnDestroy);
            PlayClose(_imagePopupBombBackdrop, _transformPopupBombAnim);
        }

        // A slow alpha yoyo that runs for as long as the screen is up.
        private void PlayVignette()
        {
            Image vignette = _imagePopupBombVignette;
            vignette.DOKill();

            Color c = vignette.color;
            vignette.color = new Color(c.r, c.g, c.b, Juice.VignetteMinAlpha);
            vignette.DOFade(Juice.VignettePeakAlpha, Juice.VignetteBreatheDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(vignette.gameObject, LinkBehaviour.KillOnDestroy);
        }
        #endregion
    }
}
