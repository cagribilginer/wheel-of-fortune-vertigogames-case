using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One pooled tile in the zone-map strip. Purely passive: the presenter decides which background and
    /// badge a zone gets, this view only ever renders what it is told.
    /// </summary>
    public sealed class ZoneMapTileView : UIViewBase
    {
        [SerializeField] private Image _ui_image_zonemap_tile_bg_value;
        [SerializeField] private Image _ui_image_zonemap_tile_frame;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_tile_number_value;
        [SerializeField] private Image _ui_image_zonemap_tile_badge_value;

        public RectTransform Rect => (RectTransform)transform;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_zonemap_tile_bg_value, "ui_image_zonemap_tile_bg_value");
            Bind(ref _ui_image_zonemap_tile_frame, "ui_image_zonemap_tile_frame");
            Bind(ref _ui_text_zonemap_tile_number_value, "ui_text_zonemap_tile_number_value");
            Bind(ref _ui_image_zonemap_tile_badge_value, "ui_image_zonemap_tile_badge_value");
        }

        public void SetZoneNumber(int zone) => _ui_text_zonemap_tile_number_value.SetText("{0}", zone);

        public void SetBackground(Sprite background) => _ui_image_zonemap_tile_bg_value.sprite = background;

        public void SetBadge(Sprite badge)
        {
            _ui_image_zonemap_tile_badge_value.sprite = badge;
            _ui_image_zonemap_tile_badge_value.enabled = badge != null;
        }
    }
}
