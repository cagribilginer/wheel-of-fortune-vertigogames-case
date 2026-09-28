using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// What the player is holding for the current run.
    /// <para>
    /// Rewards stack by id, so winning 12 then 30 Pistol Points reads as a single "x42" row rather than two.
    /// Order is first-acquisition order, so rows never rearrange under the player mid-run.
    /// </para>
    /// <para>This is the thing the bomb takes. It is deliberately per-run and never persisted.</para>
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

        /// <summary>Raised on any change to the bank's contents, including <see cref="Clear"/>.</summary>
        public event Action Changed;

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
                BankEntry existing = _entries[index];
                // Saturates, like RewardScalingMath does for one drop: an endless run must never wrap negative.
                _entries[index] = new BankEntry(reward, (int)Math.Min((long)existing.Amount + amount, int.MaxValue));
            }
            else
            {
                _indexByReward[reward] = _entries.Count;
                _entries.Add(new BankEntry(reward, amount));
            }

            Changed?.Invoke();
        }

        public int AmountOf(RewardId reward)
        {
            return _indexByReward.TryGetValue(reward, out int index) ? _entries[index].Amount : 0;
        }

        /// <summary>Wipes the run's holdings. This is what a bomb does.</summary>
        public void Clear()
        {
            if (_entries.Count == 0) return;

            _entries.Clear();
            _indexByReward.Clear();
            Changed?.Invoke();
        }
    }
}
