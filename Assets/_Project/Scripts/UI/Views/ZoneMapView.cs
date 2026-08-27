using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The horizontal zone strip. The view owns only the scroll rect and its content transform — tile
    /// pooling and the scroll tween are the presenter's job (Day 4), so this view stays a passive shell.
    /// <para>
    /// The viewport carries a bare <see cref="RectMask2D"/> with no Image: the strip is presenter-driven,
    /// never dragged, so a raycastable viewport image would exist only to fail a hygiene check.
    /// </para>
    /// <para>
    /// The two milestone badges are static chrome, not per-zone content — their interval numbers are set
    /// once, from <c>ZoneProgressionConfig</c>, the same way the wheel's own layout is done once rather
    /// than re-derived every frame.
    /// </para>
    /// </summary>
    public sealed class ZoneMapView : UIViewBase
    {
        [SerializeField] private ScrollRect _ui_scroll_zonemap;
        [SerializeField] private RectTransform _ui_content_zonemap;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_milestone_super_value;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_milestone_safe_value;

        public ScrollRect Scroll => _ui_scroll_zonemap;
        public RectTransform Content => _ui_content_zonemap;

        protected override void CacheReferences()
        {
            Bind(ref _ui_scroll_zonemap, "ui_scroll_zonemap");
            Bind(ref _ui_content_zonemap, "ui_content_zonemap");
            Bind(ref _ui_text_zonemap_milestone_super_value, "ui_text_zonemap_milestone_super_value");
            Bind(ref _ui_text_zonemap_milestone_safe_value, "ui_text_zonemap_milestone_safe_value");
        }

        public void SetMilestoneLabels(int safeInterval, int superInterval)
        {
            _ui_text_zonemap_milestone_super_value.SetText("SUPER\nZONE {0}", superInterval);
            _ui_text_zonemap_milestone_safe_value.SetText("SAFE\nZONE {0}", safeInterval);
        }
    }
}
