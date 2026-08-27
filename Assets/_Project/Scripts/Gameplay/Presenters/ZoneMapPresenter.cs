using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The horizontal zone strip: one pooled tile per zone in the visible window, scrolled so the current
    /// zone stays centred.
    /// <para>
    /// The window is built <em>ahead</em> of the player rather than growing tile-by-tile as each zone is
    /// reached — a designer wants to be able to see zone 30's golden marker while still standing on zone 1.
    /// A zone's number and type never change between runs, so tiles are never released on a reset either:
    /// the strip only ever grows, and a reset just moves the highlight back to zone 1.
    /// </para>
    /// </summary>
    public sealed class ZoneMapPresenter
    {
        private const int LookaheadZones = 15;
        private const int MinimumWindow = 30;

        private readonly ZoneMapView _view;
        private readonly ObjectPool<ZoneMapTileView> _pool;
        private readonly List<ZoneMapTileView> _active = new List<ZoneMapTileView>();

        public ZoneMapPresenter(ZoneMapView view, ZoneMapTileView tilePrefab)
        {
            _view = view;
            _pool = new ObjectPool<ZoneMapTileView>(
                () => Object.Instantiate(tilePrefab, _view.Content),
                tile => tile.gameObject.SetActive(true),
                tile => tile.gameObject.SetActive(false),
                tile => Object.Destroy(tile.gameObject));
        }

        public void ShowZone(int zone, System.Action onComplete)
        {
            int horizon = Mathf.Max(MinimumWindow, zone + LookaheadZones);
            while (_active.Count < horizon) BuildTile(_active.Count + 1);

            for (int i = 0; i < _active.Count; i++)
                _active[i].Rect.localScale = (i + 1 == zone) ? Vector3.one * 1.12f : Vector3.one;

            Scroll(zone, onComplete);
        }

        private void BuildTile(int zoneNumber)
        {
            ZoneMapTileView tile = _pool.Get();
            tile.SetZoneNumber(zoneNumber);
            tile.transform.SetSiblingIndex(_active.Count);
            _active.Add(tile);
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
