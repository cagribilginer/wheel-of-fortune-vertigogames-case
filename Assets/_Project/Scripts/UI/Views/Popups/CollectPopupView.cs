using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Run;

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
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_cash_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_gold_value;
        [SerializeField] private RectTransform _ui_content_popup_collect_list;
        [SerializeField] private Button _ui_button_popup_collect_confirm;
        [SerializeField] private Button _ui_button_popup_collect_cancel;

        private readonly CountingLabel _cash = new CountingLabel();
        private readonly CountingLabel _gold = new CountingLabel();

        public RectTransform Content
        {
            get { return _ui_content_popup_collect_list; }
        }

        public event Action ConfirmClicked;
        public event Action CancelClicked;

        #region Wiring
        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_collect_backdrop, "ui_image_popup_collect_backdrop");
            Bind(ref _ui_transform_popup_collect_anim, "ui_transform_popup_collect_anim");
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
        /// <summary>
        /// <paramref name="cash"/> and <paramref name="gold"/> are the wallet balances as they stand before
        /// this claim lands — same top-right HUD as the bomb screen, same numbers wherever a currency is
        /// shown. <see cref="PlayClaim"/> is what counts them up once the claim actually happens.
        /// </summary>
        public void Show(int zonesCleared, WalletBalances wallet)
        {
            _ui_text_popup_collect_zone_value.SetText("Cleared {0} zones", zonesCleared);
            _cash.SetTarget(wallet.Cash, Juice.CountUpDuration, _ui_text_popup_collect_cash_value, gameObject);
            _gold.SetTarget(wallet.Gold, Juice.CountUpDuration, _ui_text_popup_collect_gold_value, gameObject);

            // A fresh summary is fully interactive again.
            _ui_button_popup_collect_confirm.interactable = true;
            _ui_button_popup_collect_cancel.interactable = true;

            PlayOpen(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
        }

        /// <summary>
        /// The claim celebration: locks the buttons, punches the card and counts the balances up to
        /// <paramref name="newBalances"/>, then calls <paramref name="onComplete"/> (the run resets there) and closes.
        /// </summary>
        public void PlayClaim(WalletBalances newBalances, Action onComplete)
        {
            _ui_button_popup_collect_confirm.interactable = false;
            _ui_button_popup_collect_cancel.interactable = false;

            _ui_transform_popup_collect_anim.DOKill();
            _ui_transform_popup_collect_anim.localScale = Vector3.one;
            _ui_transform_popup_collect_anim.DOPunchScale(Vector3.one * Juice.CardPunchScale, Juice.CardPunchDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            _cash.SetTarget(newBalances.Cash, Juice.CountUpDuration, _ui_text_popup_collect_cash_value, gameObject);
            _gold.SetTarget(newBalances.Gold, Juice.CountUpDuration, _ui_text_popup_collect_gold_value, gameObject);

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

        #region Counting label
        /// <summary>
        /// One label's count-up: the first value shows outright, later ones count from what is on screen. Thousands
        /// separators go through the plain setter, since TMP's zero-alloc SetText only understands bare {0}..{4}.
        /// </summary>
        private sealed class CountingLabel
        {
            private int _shown;
            private bool _initialised;
            private Tween _tween;

            public void SetTarget(int target, float duration, TextMeshProUGUI label, GameObject owner)
            {
                _tween?.Kill();

                if (!_initialised)
                {
                    _initialised = true;
                    _shown = target;
                    label.text = target.ToString("N0");
                    return;
                }

                Tween countUp = DOVirtual.Int(_shown, target, duration, value =>
                    {
                        _shown = value;
                        label.text = value.ToString("N0");
                    })
                    .SetEase(Ease.OutCubic)
                    .SetLink(owner);

                // DOTween recycles finished tweens (GameInstaller turns recycling on), so a reference kept
                // past its tween's death can end up pointing at an unrelated live tween — drop it on kill.
                countUp.OnKill(() => { if (_tween == countUp) _tween = null; });
                _tween = countUp;
            }
        }
        #endregion
    }
}
