using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>A popup's row of bank cells: pooled, refilled from a haul and laid out before the popup opens.</summary>
    public sealed class HaulList
    {
        private readonly RewardCatalog _catalog;
        private readonly RectTransform _content;
        private readonly ObjectPool<BankEntryView> _pool;
        private readonly List<BankEntryView> _active = new List<BankEntryView>();

        public HaulList(BankEntryView entryPrefab, RectTransform content, RewardCatalog catalog)
        {
            _catalog = catalog;
            _content = content;

            _pool = new ObjectPool<BankEntryView>(
                () => Object.Instantiate(entryPrefab, _content),
                entry => entry.gameObject.SetActive(true),
                entry => entry.gameObject.SetActive(false),
                entry => Object.Destroy(entry.gameObject));
        }

        public void Show(IReadOnlyList<BankEntry> haul)
        {
            for (int i = 0; i < _active.Count; i++) _pool.Release(_active[i]);
            _active.Clear();

            for (int i = 0; i < haul.Count; i++)
            {
                BankEntryView entry = _pool.Get();
                entry.SetEntry(_catalog.IconFor(haul[i].Reward), haul[i].Amount);
                entry.transform.SetSiblingIndex(i);
                _active.Add(entry);
            }

            // The ScrollRect needs the real content size on the frame the popup opens.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }
    }
}
