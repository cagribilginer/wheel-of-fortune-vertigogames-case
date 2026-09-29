using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The state of one playthrough: which zone the player is on, what phase they are in, and what they
    /// are holding — everything the presenters render is derived from here. Restarting is a
    /// <see cref="ResetRun"/> call, not a scene reload, so nothing depends on scene-instance lifetime.
    /// </summary>
    public sealed class RunModel
    {
        private readonly IZoneClassifier _classifier;
        private readonly Wallet _wallet;

        // The currency Balances reports as gold, and ContinueService prices revives in.
        private readonly RewardId _goldCurrency;

        // The currency Balances reports as cash — the same wallet balance shown everywhere as "cash".
        private readonly RewardId _cashCurrency;

        // Every reward that lands in the wallet on cash-out. Gold and cash are always members.
        private readonly HashSet<RewardId> _currencies;

        private int _currentZone = 1;
        private RunPhase _phase = RunPhase.Idle;
        private int _goldRevivesUsed;
        private int _adRevivesUsed;

        // The haul the last bomb took, snapshotted before the bank was cleared; a revive restores it, a
        // restart discards it. Null when no bomb is pending an answer.
        private List<BankEntry> _lostHaul;

        public RunModel(
            IZoneClassifier classifier, Wallet wallet, RewardId goldCurrency, RewardId cashCurrency,
            IEnumerable<RewardId> currencies = null)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _goldCurrency = goldCurrency;
            _cashCurrency = cashCurrency;
            _currencies = new HashSet<RewardId>(currencies ?? Array.Empty<RewardId>()) { goldCurrency, cashCurrency };

            Bank = new RewardBank();
        }

        #region Events and state
        public event Action<int> ZoneChanged;
        public event Action<RunPhase> PhaseChanged;

        public RewardBank Bank { get; }

        public int CurrentZone
        {
            get { return _currentZone; }
        }

        /// <summary>Paid gold revives taken this run. Drives the doubling price of the next one.</summary>
        public int GoldRevivesUsedThisRun
        {
            get { return _goldRevivesUsed; }
        }

        /// <summary>Free ad revives taken this run. Capped by <see cref="ContinueService"/>.</summary>
        public int AdRevivesUsedThisRun
        {
            get { return _adRevivesUsed; }
        }

        /// <summary>Total revives (gold + ad) taken this run.</summary>
        public int ContinuesUsedThisRun
        {
            get { return _goldRevivesUsed + _adRevivesUsed; }
        }

        /// <summary>
        /// What the pending bomb took, for the game-over screen to show as "what you stand to lose".
        /// Empty unless a bomb is currently waiting on a revive-or-restart decision.
        /// </summary>
        public IReadOnlyList<BankEntry> LostHaul
        {
            get { return _lostHaul ?? (IReadOnlyList<BankEntry>)Array.Empty<BankEntry>(); }
        }

        /// <summary>
        /// The persistent gold and cash balances as one value, for handing to the presentation. Same wallet,
        /// same rules, just a different id per currency.
        /// </summary>
        public WalletBalances Balances
        {
            get { return new WalletBalances(_wallet.BalanceOf(_goldCurrency), _wallet.BalanceOf(_cashCurrency)); }
        }

        public ZoneType CurrentZoneType
        {
            get { return _classifier.Classify(_currentZone); }
        }

        public RunPhase Phase
        {
            get { return _phase; }
            set
            {
                if (_phase == value) return;

                _phase = value;
                PhaseChanged?.Invoke(_phase);
            }
        }

        public bool CanSpin
        {
            get { return CashOutPolicy.CanSpin(_phase); }
        }

        public bool CanLeave
        {
            get { return CashOutPolicy.CanLeave(_phase, !Bank.IsEmpty, CurrentZoneType); }
        }
        #endregion

        #region Transitions
        /// <summary>Banks a non-bomb spin result.</summary>
        public void Grant(SpinOutcome outcome)
        {
            if (outcome.IsBomb)
                throw new InvalidOperationException("Grant was called with a bomb outcome; call Detonate instead.");

            Bank.Add(outcome.Reward, outcome.Amount);
        }

        public void AdvanceZone()
        {
            _currentZone++;
            ZoneChanged?.Invoke(_currentZone);
        }

        /// <summary>
        /// Warp the run straight to a zone. Only the debug overlay calls this — the normal flow moves one
        /// zone at a time through <see cref="AdvanceZone"/>. The haul and phase are left as they are; the
        /// caller re-enters zone setup to rebuild the wheel.
        /// </summary>
        public void JumpToZone(int zone)
        {
            if (zone < 1) throw new ArgumentOutOfRangeException(nameof(zone), zone, "Zones are 1-indexed.");
            if (zone == _currentZone) return;

            _currentZone = zone;
            ZoneChanged?.Invoke(_currentZone);
        }

        /// <summary>The bomb: the entire haul is lost and the run ends. The wallet is untouched.</summary>
        public void Detonate()
        {
            _lostHaul = new List<BankEntry>(Bank.Entries);
            Bank.Clear();
            Phase = RunPhase.GameOver;
        }

        /// <summary>
        /// Survive the bomb with a paid gold revive: stay on the same zone, haul restored. The purchase
        /// itself is the caller's (ContinueService) business; this records the consequence and bumps the
        /// per-run gold-revive count that makes the next one cost double.
        /// </summary>
        public void ApplyGoldRevive()
        {
            _goldRevivesUsed++;
            RestoreLostHaul();
            Phase = RunPhase.Idle;
        }

        /// <summary>
        /// Survive the bomb with a free ad revive: same effect as <see cref="ApplyGoldRevive"/> but bumps
        /// the ad-revive count instead, which <see cref="ContinueService"/> caps per run.
        /// </summary>
        public void ApplyAdRevive()
        {
            _adRevivesUsed++;
            RestoreLostHaul();
            Phase = RunPhase.Idle;
        }

        /// <summary>
        /// Pours the snapshotted bomb haul back into the bank. A no-op when nothing is pending — so calling
        /// a revive without a preceding <see cref="Detonate"/> leaves the bank alone.
        /// </summary>
        private void RestoreLostHaul()
        {
            if (_lostHaul == null) return;

            for (int i = 0; i < _lostHaul.Count; i++)
            {
                BankEntry entry = _lostHaul[i];
                Bank.Add(entry.Reward, entry.Amount);
            }

            _lostHaul = null;
        }

        /// <summary>
        /// Walk away with the haul. Banked gold and cash convert into the persistent wallet; everything else
        /// (weapons, cosmetics, chests) is left behind with the rest of the run. This is the only way either
        /// wallet balance ever grows — so a continue is always paid for by a previous successful run.
        /// </summary>
        public void CashOut()
        {
            IReadOnlyList<BankEntry> entries = Bank.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                BankEntry entry = entries[i];
                if (entry.Amount > 0 && _currencies.Contains(entry.Reward))
                    _wallet.Add(entry.Reward, entry.Amount);
            }

            Phase = RunPhase.CashOut;
        }

        /// <summary>Back to zone 1 with an empty bank. This is what "restart" means.</summary>
        public void ResetRun()
        {
            Bank.Clear();
            _goldRevivesUsed = 0;
            _adRevivesUsed = 0;
            _lostHaul = null;

            bool zoneChanged = _currentZone != 1;
            _currentZone = 1;

            Phase = RunPhase.Idle;
            if (zoneChanged) ZoneChanged?.Invoke(_currentZone);
        }
        #endregion
    }
}
