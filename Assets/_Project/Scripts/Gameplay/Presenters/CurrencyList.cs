using System.Collections.Generic;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// A popup's currency HUD: one <see cref="CurrencyRowView"/> per wallet currency, in wallet order, with the
    /// icon and number colour taken from the catalog. Rows are created on first need and reused.
    /// </summary>
    public sealed class CurrencyList
    {
        private readonly CurrencyRowView _rowPrefab;
        private readonly RectTransform _content;
        private readonly RewardCatalog _catalog;
        private readonly List<CurrencyRowView> _rows = new();

        public CurrencyList(CurrencyRowView rowPrefab, RectTransform content, RewardCatalog catalog)
        {
            _rowPrefab = rowPrefab;
            _content = content;
            _catalog = catalog;
        }

        /// <summary>Shows the balances outright.</summary>
        public void Show(WalletBalances balances)
        {
            Sync(balances);
            for (int i = 0; i < balances.Entries.Count; i++) _rows[i].SetAmount(balances.Entries[i].Amount);
        }

        /// <summary>Counts each row up to its balance; a row that was not on screen yet shows it outright.</summary>
        public void CountTo(WalletBalances balances, float duration)
        {
            Sync(balances);
            for (int i = 0; i < balances.Entries.Count; i++) _rows[i].CountTo(balances.Entries[i].Amount, duration);
        }

        private void Sync(WalletBalances balances)
        {
            IReadOnlyList<BankEntry> entries = balances.Entries;

            while (_rows.Count < entries.Count) _rows.Add(Object.Instantiate(_rowPrefab, _content));

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < entries.Count;
                _rows[i].gameObject.SetActive(used);
                if (!used) continue;

                RewardDefinition currency = _catalog.Find(entries[i].Reward);
                _rows[i].SetCurrency(currency ? currency.Icon : null, currency ? currency.BalanceColor : Color.white);
                _rows[i].transform.SetSiblingIndex(i);
            }
        }
    }
}
