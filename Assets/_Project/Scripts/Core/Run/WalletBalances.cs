using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The persistent wallet as one value: a row per currency, in display order. Passed through the
    /// presentation seam whole so the seam does not change when a currency is added.
    /// </summary>
    public readonly struct WalletBalances
    {
        private readonly IReadOnlyList<BankEntry> _entries;

        public WalletBalances(IReadOnlyList<BankEntry> entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>One row per currency, in the order the wallet was configured with. Empty for a default value.</summary>
        public IReadOnlyList<BankEntry> Entries
        {
            get { return _entries ?? Array.Empty<BankEntry>(); }
        }

        /// <summary>The balance of <paramref name="currency"/>, or 0 when it is not a wallet currency.</summary>
        public int AmountOf(RewardId currency)
        {
            IReadOnlyList<BankEntry> entries = Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Reward == currency) return entries[i].Amount;
            }
            return 0;
        }
    }
}
