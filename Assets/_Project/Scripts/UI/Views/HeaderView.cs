using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Top strip: the persistent gold balance only. The old left-aligned "ZONE X" label was removed — the
    /// zone strip directly below already names the current zone with its raised marker, so repeating it in
    /// the header was redundant chrome.
    /// </summary>
    public sealed class HeaderView : UIViewBase
    {
        [SerializeField] private TextMeshProUGUI _ui_text_header_gold_value;

        private int _shownGold;
        private bool _initialised;
        private Tween _countTween;

        protected override void CacheReferences()
        {
            Bind(ref _ui_text_header_gold_value, "ui_text_header_gold_value");
        }

        /// <summary>
        /// The first value (wallet balance at startup) is shown outright; every later change — a continue
        /// spend, a cash-out credit — counts up/down so the claim celebration reads as the number climbing.
        /// TMP_Text.SetText's zero-alloc formatter only understands bare {0}..{4}, not ".N0", so the
        /// thousands separator goes through the plain setter.
        /// </summary>
        public void SetGold(int gold)
        {
            _countTween?.Kill();

            if (!_initialised)
            {
                _initialised = true;
                _shownGold = gold;
                _ui_text_header_gold_value.text = gold.ToString("N0");
                return;
            }

            _countTween = DOVirtual.Int(_shownGold, gold, 0.5f, value =>
                {
                    _shownGold = value;
                    _ui_text_header_gold_value.text = value.ToString("N0");
                })
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Hidden during the wheel loop, shown with the end-of-run screens. The gold subscription keeps
        /// running against the inactive object, so the balance is already current when it reappears.
        /// </summary>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
