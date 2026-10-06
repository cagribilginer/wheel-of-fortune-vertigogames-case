using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One pooled tile in the zone strip. Purely passive: the presenter decides its number, colour, weight and whether
    /// it is current. The strip is one dark bar, so a tile is just the number plus a raised white marker on one tile.
    /// </summary>
    public sealed class ZoneMapTileViewMono : UIViewBaseMono
    {
        [SerializeField] private Image _imageZonemapTileMarkerValue;
        [SerializeField] private TextMeshProUGUI _textZonemapTileNumberValue;

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
            Bind(ref _imageZonemapTileMarkerValue, "ui_image_zonemap_tile_marker_value");
            Bind(ref _textZonemapTileNumberValue, "ui_text_zonemap_tile_number_value");
        }

        public void SetZoneNumber(int zone)
        {
            _textZonemapTileNumberValue.SetText("{0}", zone);
        }

        /// <summary>A passed (or upcoming) zone: no marker, just the number in the presenter's colour/weight.</summary>
        public void SetPlain(Color numberColor, bool bold)
        {
            _imageZonemapTileMarkerValue.enabled = false;
            _textZonemapTileNumberValue.color = numberColor;
            _textZonemapTileNumberValue.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        }

        /// <summary>The zone the player is standing on: the raised white marker plus a dark, bold number.</summary>
        public void SetCurrent(Color numberColor)
        {
            _imageZonemapTileMarkerValue.enabled = true;
            _textZonemapTileNumberValue.color = numberColor;
            _textZonemapTileNumberValue.fontStyle = FontStyles.Bold;
        }
    }
}
