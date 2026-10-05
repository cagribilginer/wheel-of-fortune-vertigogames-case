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
        [SerializeField] private Image _ui_image_popup_bomb_backdrop;
        [SerializeField] private Image _ui_image_popup_bomb_vignette;
        [SerializeField] private RectTransform _ui_transform_popup_bomb_anim;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_zone_value;
        [SerializeField] private RectTransform _ui_row_popup_bomb_currency;
        [SerializeField] private RectTransform _ui_content_popup_bomb_list;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_empty_value;
        [SerializeField] private Button _ui_button_popup_bomb_giveup;
        [SerializeField] private Button _ui_button_popup_bomb_continue;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_continue_value;
        [SerializeField] private Button _ui_button_popup_bomb_advert;

        /// <summary>Where the presenter pools the lost-haul preview tiles.</summary>
        public RectTransform Content
        {
            get { return _ui_content_popup_bomb_list; }
        }

        /// <summary>Where the presenter pools the wallet's currency rows.</summary>
        public RectTransform CurrencyContent
        {
            get { return _ui_row_popup_bomb_currency; }
        }

        public event Action GiveUpClicked;
        public event Action ContinueClicked;
        public event Action AdContinueClicked;

        #region Wiring
        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_bomb_backdrop, "ui_image_popup_bomb_backdrop");
            Bind(ref _ui_image_popup_bomb_vignette, "ui_image_popup_bomb_vignette");
            Bind(ref _ui_transform_popup_bomb_anim, "ui_transform_popup_bomb_anim");
            Bind(ref _ui_text_popup_bomb_zone_value, "ui_text_popup_bomb_zone_value");
            Bind(ref _ui_row_popup_bomb_currency, "ui_row_popup_bomb_currency");
            Bind(ref _ui_content_popup_bomb_list, "ui_content_popup_bomb_list");
            Bind(ref _ui_text_popup_bomb_empty_value, "ui_text_popup_bomb_empty_value");
            Bind(ref _ui_button_popup_bomb_giveup, "ui_button_popup_bomb_giveup");
            Bind(ref _ui_button_popup_bomb_continue, "ui_button_popup_bomb_continue");
            Bind(ref _ui_text_popup_bomb_continue_value, "ui_text_popup_bomb_continue_value");
            Bind(ref _ui_button_popup_bomb_advert, "ui_button_popup_bomb_advert");
        }

        private void OnEnable()
        {
            _ui_button_popup_bomb_giveup.onClick.AddListener(RaiseGiveUp);
            _ui_button_popup_bomb_continue.onClick.AddListener(RaiseContinue);
            _ui_button_popup_bomb_advert.onClick.AddListener(RaiseAdContinue);
        }

        private void OnDisable()
        {
            _ui_button_popup_bomb_giveup.onClick.RemoveListener(RaiseGiveUp);
            _ui_button_popup_bomb_continue.onClick.RemoveListener(RaiseContinue);
            _ui_button_popup_bomb_advert.onClick.RemoveListener(RaiseAdContinue);
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
            _ui_text_popup_bomb_zone_value.SetText("You reached Zone {0}", summary.ZoneReached);

            // SetText's zero-alloc formatter does not honour ":N0" (it prints the literal characters), so the
            // thousands separator has to come from the regular setter.
            _ui_text_popup_bomb_continue_value.text = summary.GoldReviveCost.ToString("N0");

            _ui_text_popup_bomb_empty_value.gameObject.SetActive(summary.LostHaul.Count == 0);

            // Every button stays in the row; an unavailable revive is disabled, not hidden.
            _ui_button_popup_bomb_continue.interactable = summary.GoldReviveOffered;
            _ui_button_popup_bomb_advert.interactable = summary.AdReviveOffered;

            PlayVignette();
            PlayOpen(_ui_image_popup_bomb_backdrop, _ui_transform_popup_bomb_anim, Juice.BombBackdropAlpha);
        }

        public void Hide()
        {
            _ui_image_popup_bomb_vignette.DOKill();
            _ui_image_popup_bomb_vignette.DOFade(0f, Juice.PopupFadeDuration)
                .SetLink(_ui_image_popup_bomb_vignette.gameObject, LinkBehaviour.KillOnDestroy);
            PlayClose(_ui_image_popup_bomb_backdrop, _ui_transform_popup_bomb_anim);
        }

        // A slow alpha yoyo that runs for as long as the screen is up.
        private void PlayVignette()
        {
            Image vignette = _ui_image_popup_bomb_vignette;
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
