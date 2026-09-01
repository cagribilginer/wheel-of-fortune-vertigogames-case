using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Keeps a <see cref="GridLayoutGroup"/> filling left-to-right (UpperLeft) while still showing an equal
    /// left and right margin. The horizontal padding is recomputed from the container's real width so a full
    /// row of cells sits centred and any leftover width is split evenly between the two edges instead of
    /// pooling on the right.
    /// <para>
    /// A plain <c>UpperCenter</c> alignment would also centre a half-full last row; this keeps every row —
    /// including the last — left-aligned under the first, which reads as an acquisition-ordered list. The
    /// recompute is needed because the grid's column count is only known once the layout system has given
    /// the container its width, and under the landscape <c>Expand</c> canvas scaler that width grows past
    /// the 1920 reference on wider displays.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class GridEdgePadding : UIBehaviour
    {
        [Tooltip("Smallest margin to keep on each side even if the row would otherwise fill the container.")]
        [SerializeField] private int _minPadding = 8;

        private GridLayoutGroup _grid;
        private RectTransform _rect;

        protected override void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = (RectTransform)transform;
        }

        protected override void OnEnable() => Apply();

        protected override void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_grid == null) _grid = GetComponent<GridLayoutGroup>();
            if (_rect == null) _rect = (RectTransform)transform;

            float width = _rect.rect.width;
            float step = _grid.cellSize.x + _grid.spacing.x;
            if (width <= 0f || step <= 0f) return;

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
