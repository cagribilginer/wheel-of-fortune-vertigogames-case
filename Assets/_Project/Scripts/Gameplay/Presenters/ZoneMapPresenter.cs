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
    /// The horizontal zone strip: one pooled tile per zone in the visible window, scrolled so the current
    /// zone stays centred.
    /// <para>
    /// The window is built <em>ahead</em> of the player rather than growing tile-by-tile as each zone is
    /// reached — a designer wants to be able to see zone 30's golden marker while still standing on zone 1.
    /// A zone's number and type never change between runs, so tiles are never released on a reset either:
    /// the strip only ever grows, and a reset just moves the highlight back to zone 1.
    /// </para>
    /// <para>
    /// Zone 1 can never sit dead-centre in the viewport — there is nothing to its left to justify a shift,
    /// and clamping to show blank space instead would look broken. <see cref="Scroll"/> already clamps to
    /// the start; what actually reads as "wrong tile highlighted" without a strong per-type visual is
    /// solved by <see cref="ApplyStyle"/> instead, not by fighting the clamp.
    /// </para>
    /// </summary>
    public sealed class ZoneMapPresenter
    {
        private const int LookaheadZones = 15;
        private const int MinimumWindow = 30;

        private static readonly Color SafeTextColor = new Color(0.55f, 0.95f, 0.6f);
        private static readonly Color SuperTextColor = new Color(1f, 0.85f, 0.35f);

        private readonly ZoneMapView _view;
        private readonly IZoneClassifier _classifier;
        private readonly Sprite _bgSprite;
        private readonly Sprite _currentSprite;
        private readonly Sprite _superSprite;
        private readonly Sprite _safeBadge;
        private readonly ObjectPool<ZoneMapTileView> _pool;
        private readonly List<ZoneMapTileView> _active = new List<ZoneMapTileView>();

        public ZoneMapPresenter(
            ZoneMapView view, ZoneMapTileView tilePrefab, IZoneClassifier classifier,
            Sprite bgSprite, Sprite currentSprite, Sprite superSprite, Sprite safeBadge,
            ZoneProgressionConfig progression)
        {
            _view = view;
            _classifier = classifier;
            _bgSprite = bgSprite;
            _currentSprite = currentSprite;
            _superSprite = superSprite;
            _safeBadge = safeBadge;

            _view.SetMilestoneLabels(progression.SafeZoneInterval, progression.SuperZoneInterval);

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
                ApplyStyle(_active[i], i + 1, isCurrent: i + 1 == zone);

            Scroll(zone, onComplete);
        }

        private void BuildTile(int zoneNumber)
        {
            ZoneMapTileView tile = _pool.Get();
            tile.SetZoneNumber(zoneNumber);
            tile.transform.SetSiblingIndex(_active.Count);
            _active.Add(tile);
        }

        private void ApplyStyle(ZoneMapTileView tile, int zoneNumber, bool isCurrent)
        {
            if (isCurrent)
            {
                tile.SetBackground(_currentSprite != null ? _currentSprite : _bgSprite);
                tile.SetBadge(null);
                tile.SetNumberColor(Color.white);
                tile.Rect.localScale = Vector3.one * 1.1f;
                return;
            }

            tile.Rect.localScale = Vector3.one;

            switch (_classifier.Classify(zoneNumber))
            {
                case ZoneType.Super:
                    tile.SetBackground(_superSprite != null ? _superSprite : _bgSprite);
                    tile.SetBadge(null);
                    tile.SetNumberColor(SuperTextColor);
                    break;
                case ZoneType.Safe:
                    tile.SetBackground(_bgSprite);
                    tile.SetBadge(_safeBadge);
                    tile.SetNumberColor(SafeTextColor);
                    break;
                default:
                    tile.SetBackground(_bgSprite);
                    tile.SetBadge(null);
                    tile.SetNumberColor(Color.white);
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
