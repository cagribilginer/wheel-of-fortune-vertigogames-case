using System;
using Vertigo.Wheel.Core.Rewards;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The reward values that survive a run: one balance per currency <see cref="RewardId"/>. A bomb never
    /// touches it, and a currency enters only by cashing out, which is what makes a revive worth having.
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
        }
    }
}
