using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Keeps a <see cref="GridLayoutGroup"/> filling left-to-right with equal left and right margins, by recomputing
    /// the padding from the container's real width. It writes from <see cref="Update"/>, not from a layout callback:
    /// a layout change made inside a layout pass is dropped and leaves the ContentSizeFitter stale.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class GridEdgePadding : UIBehaviour
    {
        [Tooltip("Smallest margin to keep on each side even if the row would otherwise fill the container.")]
        [SerializeField] private int _minPadding = 8;

        private GridLayoutGroup _grid;
        private RectTransform _rect;
        private bool _dirty = true;

        protected override void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = (RectTransform)transform;
        }

        protected override void OnEnable()
        {
            _dirty = true;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            _dirty = true;
        }

        private void Update()
        {
            if (!_dirty) return;
            _dirty = false;
            Apply();
        }

        private void Apply()
        {
            if (!_grid) _grid = GetComponent<GridLayoutGroup>();
            if (!_rect) _rect = (RectTransform)transform;

            float width = _rect.rect.width;
            float step = _grid.cellSize.x + _grid.spacing.x;
            if (width <= 0f || step <= 0f)
            {
                _dirty = true; // width not resolved yet; try again next frame
                return;
            }

            int columns = Mathf.Max(1,
                Mathf.FloorToInt((width - 2 * _minPadding + _grid.spacing.x) / step));
            float rowWidth = columns * _grid.cellSize.x + (columns - 1) * _grid.spacing.x;
            int pad = Mathf.Max(_minPadding, Mathf.FloorToInt((width - rowWidth) * 0.5f));

            if (_grid.padding.left == pad && _grid.padding.right == pad) return;

            // Setting the property (not mutating the RectOffset in place) is what marks the group for
            // rebuild. Only left/right move; the caller keeps ownership of top/bottom.
            _grid.padding = new RectOffset(pad, pad, _grid.padding.top, _grid.padding.bottom);
        }
    }
}
