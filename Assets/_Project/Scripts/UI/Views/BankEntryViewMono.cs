using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One stacked reward row. Pooled, and shared verbatim — same prefab, same grid cell size — between
    /// the bank panel and the collect popup, which is the whole reuse story behind requirement 2's data-
    /// driven wheel content extending into the presentation layer too.
    /// </summary>
    public sealed class BankEntryViewMono : UIViewBaseMono
    {
        [SerializeField] private Image _imageBankEntryFrame;
        [SerializeField] private Image _imageBankEntryIconValue;
        [SerializeField] private TextMeshProUGUI _textBankEntryAmountValue;

        private RectTransform _rect;
        private CanvasGroup _group;

        public RectTransform Rect
        {
            get
            {
                if (!_rect) _rect = (RectTransform)transform;
                return _rect;
            }
        }

        protected override void CacheReferences()
        {
            Bind(ref _imageBankEntryFrame, "ui_image_bank_entry_frame");
            Bind(ref _imageBankEntryIconValue, "ui_image_bank_entry_icon_value");
            Bind(ref _textBankEntryAmountValue, "ui_text_bank_entry_amount_value");
        }

        /// <summary>
        /// Hides the cell without deactivating it, so it keeps its slot in the layout. The bank reserves a new
        /// reward's slot this way and reveals it the moment the flying icon arrives.
        /// </summary>
        public void SetRevealed(bool revealed)
        {
            if (!_group && !TryGetComponent(out _group)) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = revealed ? 1f : 0f;
        }

        public void SetEntry(Sprite icon, int amount)
        {
            _imageBankEntryIconValue.sprite = icon;
            SetAmount(amount);
        }

        /// <summary>Just the count — the fly-in tween drives this every frame while the number climbs.</summary>
        public void SetAmount(int amount)
        {
            AmountFormat.Apply(_textBankEntryAmountValue, amount);
        }
    }
}
