using System.Collections.Generic;
using UnityEngine;
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
        private readonly EntryPool _pool;
        private readonly List<BankEntryViewMono> _active = new();

        public HaulList(BankEntryViewMono entryPrefab, RectTransform content, RewardCatalog catalog)
        {
            _catalog = catalog;
            _content = content;

            _pool = new EntryPool(entryPrefab, content);
        }

        public void Show(IReadOnlyList<BankEntry> haul)
        {
            for (int i = 0; i < _active.Count; i++) _pool.Release(_active[i]);
            _active.Clear();

            for (int i = 0; i < haul.Count; i++)
            {
                BankEntryViewMono entry = _pool.Get();
                entry.SetEntry(_catalog.IconFor(haul[i].Reward), haul[i].Amount);
                entry.transform.SetSiblingIndex(i);
                _active.Add(entry);
            }

            // The ScrollRect needs the real content size on the frame the popup opens.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }
    }
}
