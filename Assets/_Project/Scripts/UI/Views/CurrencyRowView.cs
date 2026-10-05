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
    public sealed class CurrencyRowView : UIViewBase
    {
        [SerializeField] private Image _imagePopupCurrencyIcon;
        [SerializeField] private TextMeshProUGUI _textPopupCurrencyValue;

        private int _shown;
        private bool _isInitialised;
        private Tween _tween;

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
            if (_tween != null) _tween.Kill();
            _shown = amount;
            _isInitialised = true;
            _textPopupCurrencyValue.text = amount.ToString("N0");
        }

        /// <summary>The first value shows outright, later ones count up from what is on screen.</summary>
        public void CountTo(int target, float duration)
        {
            if (!_isInitialised)
            {
                SetAmount(target);
                return;
            }

            if (_tween != null) _tween.Kill();

            Tween countUp = DOVirtual.Int(_shown, target, duration, value =>
                {
                    _shown = value;
                    _textPopupCurrencyValue.text = value.ToString("N0");
                })
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject);

            // DOTween recycles finished tweens (GameInstaller turns recycling on), so a reference kept
            // past its tween's death can end up pointing at an unrelated live tween — drop it on kill.
            countUp.OnKill(() => { if (_tween == countUp) _tween = null; });
            _tween = countUp;
        }
    }
}
