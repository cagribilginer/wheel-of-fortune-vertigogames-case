using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The horizontal zone strip: one pooled tile per zone in the visible window, scrolled so the current
    /// zone stays centred, over a single solid dark bar.
    /// <para>
    /// The window is built <em>ahead</em> of the player rather than growing tile-by-tile as each zone is
    /// reached — a designer wants to be able to see zone 30's marker while still standing on zone 1. A
    /// zone's number and type never change between runs, so tiles are never released on a reset either: the
    /// strip only ever grows, and a reset just moves the highlight back to zone 1.
    /// </para>
    /// <para>
    /// A tile has no card of its own — the reference strip is a plain dark bar of numbers. Which tile is
    /// "current" is carried entirely by the raised white marker + a dark bold number (<see cref="ApplyStyle"/>).
    /// Number colour depends only on zone type, not on passed/upcoming: green for safe zones, gold for
    /// super zones, muted grey for the rest.
    /// </para>
    /// </summary>
    public sealed class ZoneMapPresenter
    {
        private const int LookaheadZones = 15;
        private const int MinimumWindow = 30;

        // Colour is driven by zone type only, never by whether a zone is passed or upcoming: green is
        // reserved strictly for safe zones (5, 10, 15…), gold for super zones, muted grey for everything
        // else. A passed normal zone therefore looks exactly like an upcoming one.
        private static readonly Color CurrentTextColor = new Color(0.12f, 0.13f, 0.16f);
        private static readonly Color SafeTextColor = new Color(0.40f, 0.95f, 0.45f);
        private static readonly Color SuperTextColor = new Color(1f, 0.82f, 0.30f);
        private static readonly Color NormalTextColor = new Color(0.62f, 0.64f, 0.70f);

        private readonly ZoneMapView _view;
        private readonly IZoneClassifier _classifier;
        private readonly ObjectPool<ZoneMapTileView> _pool;
        private readonly List<ZoneMapTileView> _active = new List<ZoneMapTileView>();

        public ZoneMapPresenter(ZoneMapView view, ZoneMapTileView tilePrefab, IZoneClassifier classifier)
        {
            _view = view;
            _classifier = classifier;

            _pool = new ObjectPool<ZoneMapTileView>(
                () => Object.Instantiate(tilePrefab, _view.Content),
                tile => tile.gameObject.SetActive(true),
                tile => tile.gameObject.SetActive(false),
                tile => Object.Destroy(tile.gameObject));
        }

        public void ShowZone(int zone, System.Action onComplete)
        {
            _view.SetMilestoneTargets(
                _classifier.NextZoneOfType(zone, ZoneType.Safe),
                _classifier.NextZoneOfType(zone, ZoneType.Super));

            int horizon = Mathf.Max(MinimumWindow, zone + LookaheadZones);
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

        private void ApplyStyle(ZoneMapTileView tile, int zoneNumber, int currentZone)
        {
            if (zoneNumber == currentZone)
            {
                tile.SetCurrent(CurrentTextColor);
                tile.Rect.localScale = Vector3.one * 1.12f;
                return;
            }

            tile.Rect.localScale = Vector3.one;

            switch (_classifier.Classify(zoneNumber))
            {
                case ZoneType.Super:
                    tile.SetPlain(SuperTextColor, bold: true);
                    break;
                case ZoneType.Safe:
                    tile.SetPlain(SafeTextColor, bold: true);
                    break;
                default:
                    tile.SetPlain(NormalTextColor, bold: false);
                    break;
            }
        }

        private void Scroll(int zone, System.Action onComplete)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Content);

            if (zone < 1 || zone > _active.Count) { onComplete(); return; }

            RectTransform tileRect = _active[zone - 1].Rect;
            float viewportWidth = _view.Scroll.viewport.rect.width;
            float contentWidth = _view.Content.rect.width;

            float target = viewportWidth * 0.5f - tileRect.anchoredPosition.x;
            float minX = Mathf.Min(0f, viewportWidth - contentWidth);
            target = Mathf.Clamp(target, minX, 0f);

            _view.Content.DOKill();
            _view.Content.DOAnchorPosX(target, 0.45f).SetEase(Ease.OutCubic).OnComplete(() => onComplete());
        }
    }
}
