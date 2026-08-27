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
    /// </summary>
    public sealed class ZoneMapView : UIViewBase
    {
        [SerializeField] private ScrollRect _ui_scroll_zonemap;
        [SerializeField] private RectTransform _ui_content_zonemap;

        public ScrollRect Scroll => _ui_scroll_zonemap;
        public RectTransform Content => _ui_content_zonemap;

        protected override void CacheReferences()
        {
            Bind(ref _ui_scroll_zonemap, "ui_scroll_zonemap");
            Bind(ref _ui_content_zonemap, "ui_content_zonemap");
        }
    }
}
