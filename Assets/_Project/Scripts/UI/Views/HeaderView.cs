using TMPro;
using UnityEngine;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>Top strip: current zone and the persistent gold balance. Purely passive.</summary>
    public sealed class HeaderView : UIViewBase
    {
        [SerializeField] private TextMeshProUGUI _ui_text_header_zone_value;
        [SerializeField] private TextMeshProUGUI _ui_text_header_gold_value;

        protected override void CacheReferences()
        {
            Bind(ref _ui_text_header_zone_value, "ui_text_header_zone_value");
            Bind(ref _ui_text_header_gold_value, "ui_text_header_gold_value");
        }

        public void SetZone(int zone) => _ui_text_header_zone_value.SetText("ZONE {0}", zone);

        // TMP_Text.SetText's zero-alloc formatter only understands bare {0}..{4} tokens, not .NET format
        // specifiers — "{0:N0}" was printing the literal characters "N0" instead of a thousands separator.
        public void SetGold(int gold) => _ui_text_header_gold_value.text = gold.ToString("N0");
    }
}
