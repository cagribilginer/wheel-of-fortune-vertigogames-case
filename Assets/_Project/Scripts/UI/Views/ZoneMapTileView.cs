using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One pooled tile in the zone strip. Purely passive: the presenter decides its number, colour, weight and whether
    /// it is current. The strip is one dark bar, so a tile is just the number plus a raised white marker on one tile.
    /// </summary>
    public sealed class ZoneMapTileView : UIViewBase
    {
        [SerializeField] private Image _ui_image_zonemap_tile_marker_value;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_tile_number_value;

        private RectTransform _rect;

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
            Bind(ref _ui_image_zonemap_tile_marker_value, "ui_image_zonemap_tile_marker_value");
            Bind(ref _ui_text_zonemap_tile_number_value, "ui_text_zonemap_tile_number_value");
        }

        public void SetZoneNumber(int zone)
        {
            _ui_text_zonemap_tile_number_value.SetText("{0}", zone);
        }

        /// <summary>A passed (or upcoming) zone: no marker, just the number in the presenter's colour/weight.</summary>
        public void SetPlain(Color numberColor, bool bold)
        {
            _ui_image_zonemap_tile_marker_value.enabled = false;
            _ui_text_zonemap_tile_number_value.color = numberColor;
            _ui_text_zonemap_tile_number_value.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }

        /// <summary>The zone the player is standing on: the raised white marker plus a dark, bold number.</summary>
        public void SetCurrent(Color numberColor)
        {
            _ui_image_zonemap_tile_marker_value.enabled = true;
            _ui_text_zonemap_tile_number_value.color = numberColor;
            _ui_text_zonemap_tile_number_value.fontStyle = FontStyles.Bold;
        }
    }
}
