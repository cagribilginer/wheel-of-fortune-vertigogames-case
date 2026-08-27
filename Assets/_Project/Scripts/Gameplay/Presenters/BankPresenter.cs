using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The collected-rewards grid: a pooled <see cref="BankEntryView"/> per stacked reward, rebuilt from
    /// <see cref="RewardBank"/> whenever it changes, plus the ghost that visibly carries a fresh reward from
    /// the wheel into its grid cell.
    /// <para>
    /// The ghost is a plain temporary Image parented on the canvas root — never inside the
    /// GridLayoutGroup-controlled content — because a layout rebuild would fight any tween applied to one of
    /// its own children.
    /// </para>
    /// </summary>
    public sealed class BankPresenter
    {
        private readonly BankView _view;
        private readonly RewardCatalog _catalog;
        private readonly RewardBank _bank;
        private readonly Transform _flightLayer;
        private readonly ObjectPool<BankEntryView> _pool;
        private readonly List<BankEntryView> _active = new List<BankEntryView>();

        public BankPresenter(BankView view, BankEntryView entryPrefab, RewardCatalog catalog, RewardBank bank, Transform flightLayer)
        {
            _view = view;
            _catalog = catalog;
            _bank = bank;
            _flightLayer = flightLayer;

            _pool = new ObjectPool<BankEntryView>(
                () => UnityEngine.Object.Instantiate(entryPrefab, _view.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => UnityEngine.Object.Destroy(e.gameObject));
        }

        public void Refresh()
        {
            for (int i = 0; i < _active.Count; i++) _pool.Release(_active[i]);
            _active.Clear();

            IReadOnlyList<BankEntry> entries = _bank.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                BankEntryView entry = _pool.Get();
                entry.SetEntry(_catalog.IconFor(entries[i].Reward), entries[i].Amount);
                entry.transform.SetSiblingIndex(i);
                _active.Add(entry);
            }

            _view.SetEmpty(entries.Count == 0);
        }

        public void FlyIn(SpinOutcome outcome, Vector3 fromWorldPosition, Action onComplete)
        {
            Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Content);

            int index = IndexOf(outcome.Reward);
            if (index < 0) { onComplete(); return; }

            RectTransform target = _active[index].Rect;

            var ghostGo = new GameObject("bank_fly_ghost", typeof(RectTransform), typeof(Image));
            var ghostRect = (RectTransform)ghostGo.transform;
            ghostRect.SetParent(_flightLayer, false);
            ghostRect.sizeDelta = new Vector2(72f, 72f);
            ghostRect.position = fromWorldPosition;

            Image image = ghostGo.GetComponent<Image>();
            image.sprite = _catalog.IconFor(outcome.Reward);
            image.preserveAspect = true;
            image.raycastTarget = false;

            ghostRect.DOMove(target.position, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
            {
                UnityEngine.Object.Destroy(ghostGo);
                target.DOKill();
                target.DOPunchScale(Vector3.one * 0.25f, 0.2f);
                onComplete();
            });
        }

        private int IndexOf(RewardId reward)
        {
            IReadOnlyList<BankEntry> entries = _bank.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Reward.Equals(reward)) return i;
            return -1;
        }
    }
}
