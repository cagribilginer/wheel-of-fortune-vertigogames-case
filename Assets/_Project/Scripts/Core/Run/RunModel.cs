using System;
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
        private int _continuesUsed;

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

        public int ContinuesUsedThisRun => _continuesUsed;

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

        public bool CanLeave => CashOutPolicy.CanLeave(CurrentZoneType, _phase);

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

        /// <summary>The bomb: the entire haul is lost and the run ends. The gold wallet is untouched.</summary>
        public void Detonate()
        {
            Bank.Clear();
            Phase = RunPhase.GameOver;
            RunEnded?.Invoke(RunEndReason.Bomb);
        }

        /// <summary>
        /// Spend gold to survive the bomb and stay on the same zone with the haul intact.
        /// The purchase itself is the caller's (ContinueService) business; this records the consequence.
        /// </summary>
        public void ApplyContinue()
        {
            _continuesUsed++;
            Phase = RunPhase.Idle;
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
            _continuesUsed = 0;

            bool zoneChanged = _currentZone != 1;
            _currentZone = 1;

            Phase = RunPhase.Idle;
            if (zoneChanged) ZoneChanged?.Invoke(_currentZone);
        }
    }
}
