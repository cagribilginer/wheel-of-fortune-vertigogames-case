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
    /// runs <see cref="PlayClaim"/> — the chest punch and a hold — and then hands back to the state machine
    /// to reset the run.
    /// </para>
    /// </summary>
    public sealed class CollectPopupView : PopupViewBase
    {
        [SerializeField] private Image _ui_image_popup_collect_backdrop;
        [SerializeField] private RectTransform _ui_transform_popup_collect_anim;
        [SerializeField] private Image _ui_image_popup_collect_chest_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_zone_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_cash_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_gold_value;
        [SerializeField] private RectTransform _ui_content_popup_collect_list;
        [SerializeField] private Button _ui_button_popup_collect_confirm;
        [SerializeField] private Button _ui_button_popup_collect_cancel;

        private readonly CountingLabel _cash = new CountingLabel();
        private readonly CountingLabel _gold = new CountingLabel();

        // How long the celebration holds before the run resets — long enough for the chest punch and the
        // cash/gold count-up to read, short enough not to stall the loop.
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
            Bind(ref _ui_text_popup_collect_cash_value, "ui_text_popup_collect_cash_value");
            Bind(ref _ui_text_popup_collect_gold_value, "ui_text_popup_collect_gold_value");
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

        /// <summary>
        /// <paramref name="cash"/> and <paramref name="gold"/> are the wallet balances as they stand before
        /// this claim lands — same top-right HUD as the bomb screen, same numbers wherever a currency is
        /// shown. <see cref="PlayClaim"/> is what counts them up once the claim actually happens.
        /// </summary>
        public void Show(int zonesCleared, int cash, int gold)
        {
            _ui_text_popup_collect_zone_value.SetText("Cleared {0} zones", zonesCleared);
            _cash.SetTarget(cash, _ui_text_popup_collect_cash_value, gameObject);
            _gold.SetTarget(gold, _ui_text_popup_collect_gold_value, gameObject);

            // A fresh summary is fully interactive again.
            _ui_button_popup_collect_confirm.interactable = true;
            _ui_button_popup_collect_cancel.interactable = true;

            PlayOpen(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
        }

        /// <summary>
        /// The "rewards claimed" celebration. Locks the buttons, punches the chest, counts the cash/gold row
        /// up to <paramref name="newCash"/>/<paramref name="newGold"/> (the post-claim wallet balances) right
        /// here in the popup — this is the only place the climb is shown. Holds briefly, then invokes
        /// <paramref name="onComplete"/> (the state machine resets the run there) and closes.
        /// </summary>
        public void PlayClaim(int newCash, int newGold, Action onComplete)
        {
            _ui_button_popup_collect_confirm.interactable = false;
            _ui_button_popup_collect_cancel.interactable = false;

            var chest = (RectTransform)_ui_image_popup_collect_chest_value.transform;
            chest.DOKill();
            chest.localScale = Vector3.one;
            chest.DOPunchScale(Vector3.one * 0.25f, 0.35f);

            _cash.SetTarget(newCash, _ui_text_popup_collect_cash_value, gameObject);
            _gold.SetTarget(newGold, _ui_text_popup_collect_gold_value, gameObject);

            DOVirtual.DelayedCall(ClaimHoldSeconds, () =>
            {
                onComplete?.Invoke();
                Hide();
            }).SetLink(gameObject);
        }

        public void Hide() => PlayClose(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);

        /// <summary>
        /// One label's count-up state. The first value is shown outright; every later one counts up/down
        /// from whatever is currently on screen, so the claim celebration reads as the number climbing
        /// rather than jump-cutting to the new total. Private to this view because <see cref="_cash"/> and
        /// <see cref="_gold"/> are its only two users — nothing outside this popup shows a counting balance.
        /// <para>
        /// TMP_Text.SetText's zero-alloc formatter only understands bare {0}..{4}, not ".N0", so the
        /// thousands separator goes through the plain setter.
        /// </para>
        /// </summary>
        private sealed class CountingLabel
        {
            private int _shown;
            private bool _initialised;
            private Tween _tween;

            public void SetTarget(int target, TextMeshProUGUI label, GameObject owner)
            {
                _tween?.Kill();

                if (!_initialised)
                {
                    _initialised = true;
                    _shown = target;
                    label.text = target.ToString("N0");
                    return;
                }

                _tween = DOVirtual.Int(_shown, target, 0.5f, value =>
                    {
                        _shown = value;
                        label.text = value.ToString("N0");
                    })
                    .SetEase(Ease.OutCubic)
                    .SetLink(owner);
            }
        }
    }
}
