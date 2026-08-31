using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One pooled tile in the zone-map strip. Purely passive: the presenter decides a tile's number, its
    /// text colour/weight and whether it is the current zone; this view only ever renders what it is told.
    /// <para>
    /// The strip itself is now a single solid dark bar (built in <c>MainSceneBuilder</c>), so a tile has no
    /// per-tile card of its own any more — just the number, plus one raised white "current zone" marker
    /// with a downward notch that is shown on exactly one tile at a time.
    /// </para>
    /// </summary>
    public sealed class ZoneMapTileView : UIViewBase
    {
        [SerializeField] private Image _ui_image_zonemap_tile_marker_value;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_tile_number_value;

        public RectTransform Rect => (RectTransform)transform;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_zonemap_tile_marker_value, "ui_image_zonemap_tile_marker_value");
            Bind(ref _ui_text_zonemap_tile_number_value, "ui_text_zonemap_tile_number_value");
        }

        public void SetZoneNumber(int zone) => _ui_text_zonemap_tile_number_value.SetText("{0}", zone);

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
