using System;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The reward values that survive a run, one balance per <see cref="RewardId"/> the composition root
    /// treats as a currency. Multiple currencies share this one class rather than each getting its own
    /// wallet type, because the only thing that differs between them is which save key they land on.
    /// <para>
    /// A bomb clears the <see cref="Rewards.RewardBank"/> but never touches the wallet — if it did, a bomb
    /// could lock the player out of the very continue that is meant to answer it. A currency enters the
    /// wallet only by successfully cashing out, which is what makes the continue a meta-reward for surviving.
    /// </para>
    /// </summary>
    public sealed class Wallet
    {
        private readonly ISaveService _save;

        public Wallet(ISaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        /// <summary>
        /// The PlayerPrefs key for a given currency's balance, exposed statically so editor tooling (the
        /// Reset Save menu item) can target a save slot without needing a live instance.
        /// </summary>
        public static string SaveKeyFor(RewardId currency)
        {
            return $"vertigo.wheel.wallet.{currency.Value}";
        }

        /// <summary>Raised on any change, with the currency that changed and its new balance.</summary>
        public event Action<RewardId, int> Changed;

        public int BalanceOf(RewardId currency)
        {
            return _save.GetInt(SaveKeyFor(currency));
        }

        public void Add(RewardId currency, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot add negative currency; use TrySpend.");
            if (amount == 0) return;

            // Saturates rather than wrapping: a persisted negative balance would lock out every revive for good.
            Commit(currency, (int)Math.Min((long)BalanceOf(currency) + amount, int.MaxValue));
        }

        public bool CanAfford(RewardId currency, int cost)
        {
            return cost >= 0 && BalanceOf(currency) >= cost;
        }

        public bool TrySpend(RewardId currency, int cost)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Cost cannot be negative.");
            if (!CanAfford(currency, cost)) return false;

            Commit(currency, BalanceOf(currency) - cost);
            return true;
        }

        /// <summary>Backs the Tools/Vertigo/Reset Save editor menu item.</summary>
        public void Reset(RewardId currency)
        {
            Commit(currency, 0);
        }

        private void Commit(RewardId currency, int newBalance)
        {
            _save.SetInt(SaveKeyFor(currency), newBalance);
            _save.Save();
            Changed?.Invoke(currency, newBalance);
        }
    }
}
