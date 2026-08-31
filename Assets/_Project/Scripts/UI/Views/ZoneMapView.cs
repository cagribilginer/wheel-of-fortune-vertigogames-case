using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The horizontal zone strip. The view owns only the scroll rect and its content transform — tile
    /// pooling and the scroll tween are the presenter's job, so this view stays a passive shell.
    /// <para>
    /// The viewport carries a bare <see cref="RectMask2D"/> with no Image: the strip is presenter-driven,
    /// never dragged, so a raycastable viewport image would exist only to fail a hygiene check.
    /// </para>
    /// <para>
    /// The two top-right milestone badges ("SUPER ZONE 30", "SAFE ZONE 10") are not static chrome any more:
    /// their target numbers count up as the player advances, so the presenter re-labels them on every zone
    /// change rather than once at startup.
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

        /// <summary>
        /// <paramref name="nextSafeZone"/> / <paramref name="nextSuperZone"/> are absolute zone numbers, not
        /// intervals — the next safe/super zone strictly ahead of where the player is now.
        /// </summary>
        public void SetMilestoneTargets(int nextSafeZone, int nextSuperZone)
        {
            _ui_text_zonemap_milestone_super_value.SetText("SUPER ZONE {0}", nextSuperZone);
            _ui_text_zonemap_milestone_safe_value.SetText("SAFE ZONE {0}", nextSafeZone);
        }
    }
}
