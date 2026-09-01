using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The cash-out confirmation. The reward list is pooled <see cref="BankEntryView"/> instances driven
    /// by the presenter into <see cref="Content"/> — the same prefab and grid layout as the bank panel.
    /// <para>
    /// Nothing is committed while this is open: <see cref="CancelClicked"/> (the corner X) drops the player
    /// straight back onto the wheel with their haul intact; <see cref="ConfirmClicked"/> ("CLAIM &amp; LEAVE")
    /// runs <see cref="PlayClaim"/> — the chest punch, the "added to inventory" banner, a hold — and then
    /// hands back to the state machine to reset the run.
    /// </para>
    /// </summary>
    public sealed class CollectPopupView : PopupViewBase
    {
        [SerializeField] private Image _ui_image_popup_collect_backdrop;
        [SerializeField] private RectTransform _ui_transform_popup_collect_anim;
        [SerializeField] private Image _ui_image_popup_collect_chest_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_zone_value;
        [SerializeField] private RectTransform _ui_panel_popup_collect_banner;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_banner_value;
        [SerializeField] private RectTransform _ui_content_popup_collect_list;
        [SerializeField] private Button _ui_button_popup_collect_confirm;
        [SerializeField] private Button _ui_button_popup_collect_cancel;

        // How long the celebration holds before the run resets — long enough for the chest punch and the
        // header's gold count-up to read, short enough not to stall the loop.
        private const float ClaimHoldSeconds = 0.8f;

        public RectTransform Content => _ui_content_popup_collect_list;

        public event Action ConfirmClicked;
        public event Action CancelClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_collect_backdrop, "ui_image_popup_collect_backdrop");
            Bind(ref _ui_transform_popup_collect_anim, "ui_transform_popup_collect_anim");
            Bind(ref _ui_image_popup_collect_chest_value, "ui_image_popup_collect_chest_value");
            Bind(ref _ui_text_popup_collect_zone_value, "ui_text_popup_collect_zone_value");
            Bind(ref _ui_panel_popup_collect_banner, "ui_panel_popup_collect_banner");
            Bind(ref _ui_text_popup_collect_banner_value, "ui_text_popup_collect_banner_value");
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

        private void RaiseConfirm() => ConfirmClicked?.Invoke();
        private void RaiseCancel() => CancelClicked?.Invoke();

        public void SetChest(Sprite chest) => _ui_image_popup_collect_chest_value.sprite = chest;

        public void Show(int zonesCleared)
        {
            _ui_text_popup_collect_zone_value.SetText("Cleared {0} zones", zonesCleared);

            // A fresh summary is fully interactive again and the celebration banner is back down.
            _ui_button_popup_collect_confirm.interactable = true;
            _ui_button_popup_collect_cancel.interactable = true;
            _ui_panel_popup_collect_banner.gameObject.SetActive(false);

            PlayOpen(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
        }

        /// <summary>
        /// The "rewards claimed" celebration. Locks the buttons, punches the chest, reveals the recap
        /// banner, holds briefly, then invokes <paramref name="onComplete"/> (the state machine resets the
        /// run there) and closes.
        /// </summary>
        public void PlayClaim(Action onComplete)
        {
            _ui_button_popup_collect_confirm.interactable = false;
            _ui_button_popup_collect_cancel.interactable = false;

            var chest = (RectTransform)_ui_image_popup_collect_chest_value.transform;
            chest.DOKill();
            chest.localScale = Vector3.one;
            chest.DOPunchScale(Vector3.one * 0.25f, 0.35f);

            _ui_panel_popup_collect_banner.gameObject.SetActive(true);
            _ui_text_popup_collect_banner_value.alpha = 0f;
            _ui_text_popup_collect_banner_value.DOKill();
            _ui_text_popup_collect_banner_value.DOFade(1f, 0.3f);

            DOVirtual.DelayedCall(ClaimHoldSeconds, () =>
            {
                onComplete?.Invoke();
                Hide();
            }).SetLink(gameObject);
        }

        public void Hide() => PlayClose(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
    }
}
