using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The bomb defeat / revive screen. No modal card — the title, the lost haul and the three buttons sit
    /// straight on a near-black backdrop with a breathing red vignette. Shows the haul the bomb just took
    /// (so the player sees what a revive wins back) and the currency HUD in the corner.
    /// <para>
    /// All three buttons are always in the layout; a revive path that isn't currently available is shown
    /// disabled rather than removed, so the row never reflows.
    /// </para>
    /// </summary>
    public sealed class BombPopupView : PopupViewBase
    {
        [SerializeField] private Image _ui_image_popup_bomb_backdrop;
        [SerializeField] private Image _ui_image_popup_bomb_vignette;
        [SerializeField] private RectTransform _ui_transform_popup_bomb_anim;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_zone_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_cash_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_gold_value;
        [SerializeField] private RectTransform _ui_content_popup_bomb_list;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_empty_value;
        [SerializeField] private Button _ui_button_popup_bomb_giveup;
        [SerializeField] private Button _ui_button_popup_bomb_continue;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_continue_value;
        [SerializeField] private Button _ui_button_popup_bomb_advert;

        // Dark, but not opaque: the pre-bomb bank panel stays faintly visible behind it so a revive reads
        // as "you kept your haul", and the red vignette carries the defeat mood on top.
        private const float BackdropAlpha = 0.86f;

        /// <summary>Where the presenter pools the lost-haul preview tiles.</summary>
        public RectTransform Content => _ui_content_popup_bomb_list;

        public event Action GiveUpClicked;
        public event Action ContinueClicked;
        public event Action AdContinueClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_bomb_backdrop, "ui_image_popup_bomb_backdrop");
            Bind(ref _ui_image_popup_bomb_vignette, "ui_image_popup_bomb_vignette");
            Bind(ref _ui_transform_popup_bomb_anim, "ui_transform_popup_bomb_anim");
            Bind(ref _ui_text_popup_bomb_zone_value, "ui_text_popup_bomb_zone_value");
            Bind(ref _ui_text_popup_bomb_cash_value, "ui_text_popup_bomb_cash_value");
            Bind(ref _ui_text_popup_bomb_gold_value, "ui_text_popup_bomb_gold_value");
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

        private void RaiseGiveUp() => GiveUpClicked?.Invoke();
        private void RaiseContinue() => ContinueClicked?.Invoke();
        private void RaiseAdContinue() => AdContinueClicked?.Invoke();

        public void Show(
            int zoneReached, int lostRewardCount, int playerCash, int playerGold,
            bool goldReviveOffered, int goldReviveCost, bool adReviveOffered)
        {
            _ui_text_popup_bomb_zone_value.SetText("You reached Zone {0}", zoneReached);

            // SetText's zero-alloc formatter does not honour ":N0" (it prints the literal characters), so the
            // thousands separator has to come from the regular setter.
            _ui_text_popup_bomb_cash_value.text = playerCash.ToString("N0");
            _ui_text_popup_bomb_gold_value.text = playerGold.ToString("N0");
            _ui_text_popup_bomb_continue_value.text = goldReviveCost.ToString("N0");

            _ui_text_popup_bomb_empty_value.gameObject.SetActive(lostRewardCount == 0);

            // Every button stays in the row; an unavailable revive is disabled, not hidden.
            _ui_button_popup_bomb_continue.interactable = goldReviveOffered;
            _ui_button_popup_bomb_advert.interactable = adReviveOffered;

            PlayVignette();
            PlayOpen(_ui_image_popup_bomb_backdrop, _ui_transform_popup_bomb_anim, BackdropAlpha);
        }

        public void Hide()
        {
            _ui_image_popup_bomb_vignette.DOKill();
            _ui_image_popup_bomb_vignette.DOFade(0f, 0.2f);
            PlayClose(_ui_image_popup_bomb_backdrop, _ui_transform_popup_bomb_anim);
        }

        // The breathing red vignette: a slow alpha yoyo that runs for as long as the screen is up.
        private void PlayVignette()
        {
            Image vignette = _ui_image_popup_bomb_vignette;
            vignette.DOKill();

            Color c = vignette.color;
            vignette.color = new Color(c.r, c.g, c.b, 0.5f);
            vignette.DOFade(0.9f, 0.85f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(vignette.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
