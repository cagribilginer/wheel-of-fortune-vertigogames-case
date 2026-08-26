using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Tests.EditMode.Doubles
{
    /// <summary>
    /// Completes every animation synchronously and records what it was asked to show.
    /// <para>
    /// This is the seam that makes the whole flow testable: a sixty-zone run finishes in microseconds with
    /// no scene, no tweens and no frames, and the assertions can be about what the player would have been
    /// shown rather than about internal fields.
    /// </para>
    /// </summary>
    public class InstantPresentation : IWheelPresentation
    {
        public int ZonesShown { get; private set; }
        public int LastZoneShown { get; private set; }
        public ZoneType LastZoneType { get; private set; }
        public WheelModel LastWheel { get; private set; }

        public bool CanSpin { get; private set; }
        public bool CanLeave { get; private set; }
        public bool CanGiveUp { get; private set; }

        public int SpinsPlayed { get; private set; }
        public int LastSlotIndex { get; private set; } = -1;
        public int BombsPlayed { get; private set; }
        public int RewardsGranted { get; private set; }

        public bool GameOverVisible { get; private set; }
        public bool ContinueOffered { get; private set; }
        public int ContinueCostShown { get; private set; }
        public int GameOverZoneShown { get; private set; }

        public bool CashOutVisible { get; private set; }
        public int CashOutZonesCleared { get; private set; }
        public List<BankEntry> CashOutHaul { get; } = new List<BankEntry>();

        public bool GiveUpConfirmVisible { get; private set; }
        public int GiveUpRewardsAtStake { get; private set; }

        public virtual void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete)
        {
            ZonesShown++;
            LastZoneShown = zone;
            LastZoneType = zoneType;
            LastWheel = wheel;
            onComplete?.Invoke();
        }

        public virtual void SetInputState(bool canSpin, bool canLeave, bool canGiveUp)
        {
            CanSpin = canSpin;
            CanLeave = canLeave;
            CanGiveUp = canGiveUp;
        }

        public virtual void PlaySpin(int slotIndex, Action onComplete)
        {
            SpinsPlayed++;
            LastSlotIndex = slotIndex;
            onComplete?.Invoke();
        }

        public virtual void PlayReveal(SpinOutcome outcome, Action onComplete) => onComplete?.Invoke();

        public virtual void PlayRewardGranted(SpinOutcome outcome, Action onComplete)
        {
            RewardsGranted++;
            onComplete?.Invoke();
        }

        public virtual void PlayBomb(Action onComplete)
        {
            BombsPlayed++;
            onComplete?.Invoke();
        }

        public virtual void ShowGameOver(int zoneReached, bool continueOffered, int continueCost)
        {
            GameOverVisible = true;
            GameOverZoneShown = zoneReached;
            ContinueOffered = continueOffered;
            ContinueCostShown = continueCost;
        }

        public virtual void HideGameOver() => GameOverVisible = false;

        public virtual void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared)
        {
            CashOutVisible = true;
            CashOutZonesCleared = zonesCleared;
            CashOutHaul.Clear();
            for (int i = 0; i < haul.Count; i++) CashOutHaul.Add(haul[i]);
        }

        public virtual void HideCashOut() => CashOutVisible = false;

        public virtual void ShowGiveUpConfirm(int rewardsAtStake)
        {
            GiveUpConfirmVisible = true;
            GiveUpRewardsAtStake = rewardsAtStake;
        }

        public virtual void HideGiveUpConfirm() => GiveUpConfirmVisible = false;
    }
}
