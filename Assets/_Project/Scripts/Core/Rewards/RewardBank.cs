using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// What the player holds for the current run, stacked by id in first-acquisition order.
    /// The bomb takes all of it; it is per-run and never persisted.
    /// </summary>
    public sealed class RewardBank
    {
        private readonly List<BankEntry> _entries = new List<BankEntry>();
        private readonly ReadOnlyCollection<BankEntry> _entriesView;
        private readonly Dictionary<RewardId, int> _indexByReward = new Dictionary<RewardId, int>();

        public RewardBank()
        {
            _entriesView = new ReadOnlyCollection<BankEntry>(_entries);
        }

        /// <summary>A genuine read-only view: casting it back to a List and mutating it is not possible.</summary>
        public IReadOnlyList<BankEntry> Entries
        {
            get { return _entriesView; }
        }

        public int DistinctRewardCount
        {
            get { return _entries.Count; }
        }

        public bool IsEmpty
        {
            get { return _entries.Count == 0; }
        }

        public void Add(RewardId reward, int amount)
        {
            if (reward.IsEmpty)
                throw new ArgumentException("Cannot bank an empty RewardId.", nameof(reward));
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Banked amount must be >= 1.");

            if (_indexByReward.TryGetValue(reward, out int index))
            {
                // Saturates, like RewardScalingMath does for one drop: an endless run must never wrap negative.
                long stacked = (long)_entries[index].Amount + amount;
                _entries[index] = new BankEntry(reward, (int)Math.Min(stacked, int.MaxValue));
            }
            else
            {
                _indexByReward[reward] = _entries.Count;
                _entries.Add(new BankEntry(reward, amount));
            }
        }

        public int AmountOf(RewardId reward)
        {
            return _indexByReward.TryGetValue(reward, out int index) ? _entries[index].Amount : 0;
        }

        /// <summary>Wipes the run's holdings. This is what a bomb does.</summary>
        public void Clear()
        {
            _entries.Clear();
            _indexByReward.Clear();
        }
    }
}
