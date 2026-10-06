using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One wallet currency in a popup's HUD: an icon and its balance. Pooled by the presenter and shared by the
    /// bomb and cash-out popups, so a new currency is a catalog asset, not a new label in each view.
    /// </summary>
    public sealed class CurrencyRowViewMono : UIViewBaseMono
    {
        [SerializeField] private Image _imagePopupCurrencyIcon;
        [SerializeField] private TextMeshProUGUI _textPopupCurrencyValue;

        private int _shown;
        private bool _isInitialised;

        protected override void CacheReferences()
        {
            Bind(ref _imagePopupCurrencyIcon, "ui_image_popup_currency_icon");
            Bind(ref _textPopupCurrencyValue, "ui_text_popup_currency_value");
        }

        public void SetCurrency(Sprite icon, Color valueColor)
        {
            _imagePopupCurrencyIcon.sprite = icon;
            _textPopupCurrencyValue.color = valueColor;
        }

        /// <summary>Shows the balance outright. The regular setter is needed for the thousands separator: TMP's zero-alloc SetText does not honour ":N0".</summary>
        public void SetAmount(int amount)
        {
            DOTween.Kill(this);
            _shown = amount;
            _isInitialised = true;
            _textPopupCurrencyValue.text = amount.ToString(ViewText.AMOUNT_FORMAT);
        }

        /// <summary>The first value shows outright, later ones count up from what is on screen.</summary>
        public void CountTo(int target, float duration)
        {
            if (!_isInitialised)
            {
                SetAmount(target);
                return;
            }

            // Found again by target, never by a kept reference: DOTween recycles finished tweens, so a stored
            // one could end up pointing at an unrelated live tween.
            DOTween.Kill(this);

            DOVirtual.Int(_shown, target, duration, ShowCountedAmount)
                .SetEase(Ease.OutCubic)
                .SetTarget(this)
                .SetLink(gameObject);
        }

        private void ShowCountedAmount(int value)
        {
            _shown = value;
            _textPopupCurrencyValue.text = value.ToString(ViewText.AMOUNT_FORMAT);
        }
    }
}
