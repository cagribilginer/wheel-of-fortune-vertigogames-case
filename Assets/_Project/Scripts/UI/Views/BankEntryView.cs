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
    public sealed class BankEntryView : UIViewBase
    {
        [SerializeField] private Image _ui_image_bank_entry_frame;
        [SerializeField] private Image _ui_image_bank_entry_icon_value;
        [SerializeField] private TextMeshProUGUI _ui_text_bank_entry_amount_value;

        public RectTransform Rect => (RectTransform)transform;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_bank_entry_frame, "ui_image_bank_entry_frame");
            Bind(ref _ui_image_bank_entry_icon_value, "ui_image_bank_entry_icon_value");
            Bind(ref _ui_text_bank_entry_amount_value, "ui_text_bank_entry_amount_value");
        }

        public void SetEntry(Sprite icon, int amount)
        {
            _ui_image_bank_entry_icon_value.sprite = icon;
            SetAmount(amount);
        }

        /// <summary>Just the count — the fly-in tween drives this every frame while the number climbs.</summary>
        public void SetAmount(int amount) => _ui_text_bank_entry_amount_value.SetText("x{0}", amount);
    }
}
