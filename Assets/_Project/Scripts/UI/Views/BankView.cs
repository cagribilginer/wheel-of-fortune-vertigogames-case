using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The right-column collected-rewards grid. Entry pooling is the presenter's job; this view exposes the
    /// grid content transform and the empty-state placeholder text.
    /// </summary>
    public sealed class BankView : UIViewBase
    {
        [SerializeField] private ScrollRect _ui_scroll_bank;
        [SerializeField] private RectTransform _ui_content_bank;
        [SerializeField] private TextMeshProUGUI _ui_text_bank_empty_value;

        public ScrollRect Scroll => _ui_scroll_bank;
        public RectTransform Content => _ui_content_bank;

        protected override void CacheReferences()
        {
            Bind(ref _ui_scroll_bank, "ui_scroll_bank");
            Bind(ref _ui_content_bank, "ui_content_bank");
            Bind(ref _ui_text_bank_empty_value, "ui_text_bank_empty_value");
        }

        public void SetEmpty(bool empty) => _ui_text_bank_empty_value.gameObject.SetActive(empty);
    }
}
