using UnityEngine;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Shrinks its RectTransform to <see cref="Screen.safeArea"/> and recomputes when it changes, because a landscape
    /// notch flips sides between Landscape Left and Right. It sits on a child of the Canvas, so the background can
    /// still bleed under the notch.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply(force: true);
        }

        // A screen-size int compare is negligible next to the correctness risk of a stale inset after a
        // rotation event that fires between frames.
        private void Update()
        {
            Apply(force: false);
        }

        private void Apply(bool force)
        {
            Rect safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);

            // Guards a one-frame 0x0 that some Android resume paths report, which would otherwise divide
            // by zero and drive the anchors to NaN.
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            if (!force
                && safeArea == _lastSafeArea
                && screenSize == _lastScreenSize
                && Screen.orientation == _lastOrientation)
                return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastOrientation = Screen.orientation;

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;
            min.x /= screenSize.x;
            min.y /= screenSize.y;
            max.x /= screenSize.x;
            max.y /= screenSize.y;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
