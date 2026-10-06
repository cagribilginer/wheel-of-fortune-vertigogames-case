using UnityEngine;
using UnityEngine.Pool;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>Pooled bank cells under one parent, shared by the bank grid and the popup hauls.</summary>
    public sealed class EntryPool
    {
        private readonly BankEntryViewMono _prefab;
        private readonly RectTransform _parent;
        private readonly ObjectPool<BankEntryViewMono> _pool;

        public EntryPool(BankEntryViewMono prefab, RectTransform parent)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<BankEntryViewMono>(Create, OnGet, OnRelease, OnDestroyEntry);
        }

        public BankEntryViewMono Get()
        {
            return _pool.Get();
        }

        public void Release(BankEntryViewMono entry)
        {
            _pool.Release(entry);
        }

        private BankEntryViewMono Create()
        {
            return Object.Instantiate(_prefab, _parent);
        }

        private static void OnGet(BankEntryViewMono entry)
        {
            entry.gameObject.SetActive(true);
        }

        private static void OnRelease(BankEntryViewMono entry)
        {
            entry.gameObject.SetActive(false);
        }

        private static void OnDestroyEntry(BankEntryViewMono entry)
        {
            Object.Destroy(entry.gameObject);
        }
    }
}
