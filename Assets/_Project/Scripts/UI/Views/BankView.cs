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
        [SerializeField] private ScrollRect _scrollBank;
        [SerializeField] private RectTransform _contentBank;
        [SerializeField] private TextMeshProUGUI _textBankEmptyValue;

        public RectTransform Content
        {
            get { return _contentBank; }
        }

        protected override void CacheReferences()
        {
            Bind(ref _scrollBank, "ui_scroll_bank");
            Bind(ref _contentBank, "ui_content_bank");
            Bind(ref _textBankEmptyValue, "ui_text_bank_empty_value");
        }

        public void SetEmpty(bool empty)
        {
            _textBankEmptyValue.gameObject.SetActive(empty);
        }
    }
}
