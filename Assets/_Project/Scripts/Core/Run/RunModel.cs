using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// The state of one playthrough: which zone the player is on, what phase they are in, and what they
    /// are holding. Everything the presenters render is derived from here.
    /// <para>
    /// Restarting is a <see cref="ResetRun"/> call rather than a scene reload — no transition bugs, no
    /// reallocation, no loading hitch, and nothing that depends on scene-instance lifetime.
    /// </para>
    /// </summary>
    public sealed class RunModel
    {
        private readonly IZoneClassifier _classifier;
        private readonly GoldWallet _wallet;
        private readonly RewardId _goldRewardId;

        private int _currentZone = 1;
        private RunPhase _phase = RunPhase.Idle;
        private int _goldRevivesUsed;
        private int _adRevivesUsed;

        // The haul the last bomb took, snapshotted the instant before the bank was cleared. A revive
        // (paid or ad) pours it back in; a restart or a fresh run discards it. Null whenever no bomb is
        // currently pending an answer.
        private List<BankEntry> _lostHaul;

        public RunModel(IZoneClassifier classifier, GoldWallet wallet, RewardId goldRewardId)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _goldRewardId = goldRewardId;

            Bank = new RewardBank();
        }

        public event Action<int> ZoneChanged;
        public event Action<RunPhase> PhaseChanged;
        public event Action<RunEndReason> RunEnded;

        public RewardBank Bank { get; }

        public int CurrentZone => _currentZone;

        /// <summary>Paid gold revives taken this run. Drives the doubling price of the next one.</summary>
        public int GoldRevivesUsedThisRun => _goldRevivesUsed;

        /// <summary>Free ad revives taken this run. Capped by <see cref="ContinueService"/>.</summary>
        public int AdRevivesUsedThisRun => _adRevivesUsed;

        /// <summary>Total revives (gold + ad) taken this run.</summary>
        public int ContinuesUsedThisRun => _goldRevivesUsed + _adRevivesUsed;

        /// <summary>
        /// What the pending bomb took, for the game-over screen to show as "what you stand to lose".
        /// Empty unless a bomb is currently waiting on a revive-or-restart decision.
        /// </summary>
        public IReadOnlyList<BankEntry> LostHaul => _lostHaul ?? (IReadOnlyList<BankEntry>)Array.Empty<BankEntry>();

        /// <summary>The persistent wallet balance, surfaced here so a state can hand it to the presentation.</summary>
        public int WalletBalance => _wallet.Balance;

        public ZoneType CurrentZoneType => _classifier.Classify(_currentZone);

        public RunPhase Phase
        {
            get => _phase;
            set
            {
                if (_phase == value) return;

                _phase = value;
                PhaseChanged?.Invoke(_phase);
            }
        }

        public bool CanSpin => CashOutPolicy.CanSpin(_phase);

        public bool CanLeave => CashOutPolicy.CanLeave(_phase, !Bank.IsEmpty);

        public bool CanGiveUp => CashOutPolicy.CanGiveUp(_phase);

        /// <summary>Banks a non-bomb spin result.</summary>
        public void Grant(SpinOutcome outcome, int unitValue = 1)
        {
            if (outcome.IsBomb)
                throw new InvalidOperationException("Grant was called with a bomb outcome; call Detonate instead.");

            Bank.Add(outcome.Reward, outcome.Amount, unitValue);
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

        /// <summary>The bomb: the entire haul is lost and the run ends. The gold wallet is untouched.</summary>
        public void Detonate()
        {
            _lostHaul = new List<BankEntry>(Bank.Entries);
            Bank.Clear();
            Phase = RunPhase.GameOver;
            RunEnded?.Invoke(RunEndReason.Bomb);
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
                Bank.Add(entry.Reward, entry.Amount, entry.UnitValue);
            }

            _lostHaul = null;
        }

        /// <summary>
        /// Walk away with the haul. Any banked gold converts into the persistent wallet, which is the only
        /// way the wallet ever grows — so a continue is always paid for by a previous successful run.
        /// </summary>
        public void CashOut()
        {
            if (!_goldRewardId.IsEmpty)
            {
                int bankedGold = Bank.AmountOf(_goldRewardId);
                if (bankedGold > 0) _wallet.Add(bankedGold);
            }

            Phase = RunPhase.CashOut;
            RunEnded?.Invoke(RunEndReason.CashedOut);
        }

        /// <summary>Abandon the run from a risky zone, forfeiting the haul.</summary>
        public void GiveUp()
        {
            Bank.Clear();
            Phase = RunPhase.GameOver;
            RunEnded?.Invoke(RunEndReason.GaveUp);
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
    }
}
