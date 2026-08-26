using System;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The one value that survives a run.
    /// <para>
    /// A bomb clears the <see cref="Rewards.RewardBank"/> but never touches the wallet — if it did, a bomb
    /// could lock the player out of the very continue that is meant to answer it. Gold enters the wallet
    /// only by successfully cashing out, which is what makes the continue a meta-reward for surviving.
    /// </para>
    /// </summary>
    public sealed class GoldWallet
    {
        public const string SaveKey = "vertigo.wheel.gold";

        private readonly ISaveService _save;

        public GoldWallet(ISaveService save) =>
            _save = save ?? throw new ArgumentNullException(nameof(save));

        public event Action<int> Changed;

        public int Balance => _save.GetInt(SaveKey);

        public void Add(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot add negative gold; use TrySpend.");
            if (amount == 0) return;

            Commit(Balance + amount);
        }

        public bool CanAfford(int cost) => cost >= 0 && Balance >= cost;

        public bool TrySpend(int cost)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Cost cannot be negative.");
            if (!CanAfford(cost)) return false;

            Commit(Balance - cost);
            return true;
        }

        /// <summary>Backs the Tools/Vertigo/Reset Save editor menu item.</summary>
        public void Reset() => Commit(0);

        private void Commit(int newBalance)
        {
            _save.SetInt(SaveKey, newBalance);
            _save.Save();
            Changed?.Invoke(newBalance);
        }
    }
}
