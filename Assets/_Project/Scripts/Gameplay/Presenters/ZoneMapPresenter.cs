using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The horizontal zone strip: one pooled tile per zone in a window built ahead of the player and scrolled to
    /// keep the current zone centred. The strip only grows; a reset just moves the highlight back to zone 1.
    /// A number's colour and weight come from the theme of the wheel that zone uses, like the wheel itself.
    /// </summary>
    public sealed class ZoneMapPresenter
    {
        private const int LOOKAHEAD_ZONES = 15;
        private const int MINIMUM_WINDOW = 30;

        // The scroll target that puts a tile in the middle of the viewport.
        private const float VIEWPORT_CENTER = 0.5f;

        private readonly ZoneMapView _view;
        private readonly IZoneClassifier _classifier;
        private readonly ZoneProgressionConfig _progression;
        private readonly JuiceConfig _juice;
        private readonly ObjectPool<ZoneMapTileView> _pool;
        private readonly List<ZoneMapTileView> _active = new();

        public ZoneMapPresenter(
            ZoneMapView view, ZoneMapTileView tilePrefab, IZoneClassifier classifier,
            ZoneProgressionConfig progression, JuiceConfig juice)
        {
            _view = view;
            _classifier = classifier;
            _progression = progression;
            _juice = juice;

            _pool = new ObjectPool<ZoneMapTileView>(
                () => Object.Instantiate(tilePrefab, _view.Content),
                tile => tile.gameObject.SetActive(true),
                tile => tile.gameObject.SetActive(false),
                tile => Object.Destroy(tile.gameObject));
        }

        #region Window
        public void ShowZone(int zone, System.Action onComplete)
        {
            _view.SetMilestoneTargets(
                _classifier.NextZoneOfType(zone, ZoneType.Safe),
                _classifier.NextZoneOfType(zone, ZoneType.Super));

            int horizon = Mathf.Max(MINIMUM_WINDOW, zone + LOOKAHEAD_ZONES);
            while (_active.Count < horizon) BuildTile(_active.Count + 1);

            for (int i = 0; i < _active.Count; i++)
                ApplyStyle(_active[i], i + 1, zone);

            Scroll(zone, onComplete);
        }

        private void BuildTile(int zoneNumber)
        {
            ZoneMapTileView tile = _pool.Get();
            tile.SetZoneNumber(zoneNumber);
            tile.transform.SetSiblingIndex(_active.Count);
            _active.Add(tile);
        }
        #endregion

        #region Style and scroll
        private void ApplyStyle(ZoneMapTileView tile, int zoneNumber, int currentZone)
        {
            if (zoneNumber == currentZone)
            {
                tile.SetCurrent(_juice.CurrentZoneTextColor);
                tile.Rect.localScale = Vector3.one * _juice.CurrentZoneTileScale;
                return;
            }

            tile.Rect.localScale = Vector3.one;

            WheelThemeConfig theme = _progression.ThemeFor(zoneNumber, _classifier.Classify(zoneNumber));
            if (theme) tile.SetPlain(theme.StripNumberColor, theme.StripNumberBold);
            else tile.SetPlain(Color.white, bold: false);
        }

        private void Scroll(int zone, System.Action onComplete)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Content);

            if (zone < 1 || zone > _active.Count) { onComplete(); return; }

            RectTransform tileRect = _active[zone - 1].Rect;
            float viewportWidth = _view.Scroll.viewport.rect.width;
            float contentWidth = _view.Content.rect.width;

            float target = viewportWidth * VIEWPORT_CENTER - tileRect.anchoredPosition.x;
            float minX = Mathf.Min(0f, viewportWidth - contentWidth);
            target = Mathf.Clamp(target, minX, 0f);

            _view.Content.DOComplete();
            _view.Content.DOAnchorPosX(target, _juice.ZoneScrollDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(_view.Content.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(new TweenCallback(onComplete));
        }
        #endregion
    }
}
