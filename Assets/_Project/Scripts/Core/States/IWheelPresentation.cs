using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>
    /// Everything the flow needs the screen to do, expressed without a single Unity type.
    /// <para>
    /// Each animating call takes a completion callback rather than returning: the state machine advances
    /// when the presentation says it has finished, which lets a test substitute an implementation that
    /// completes instantly and drive a sixty-zone run in microseconds with no scene and no frames.
    /// </para>
    /// </summary>
    public interface IWheelPresentation
    {
        /// <summary>Re-themes the wheel for a new zone and scrolls the zone map to it.</summary>
        void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete);

        /// <summary>Mirrors the current input legality onto the buttons. Never decides it.</summary>
        void SetInputState(bool canSpin, bool canLeave, bool canGiveUp);

        /// <summary>Rotates the wheel to a slot the logic has already committed to.</summary>
        void PlaySpin(int slotIndex, Action onComplete);

        void PlayReveal(SpinOutcome outcome, Action onComplete);

        void PlayRewardGranted(SpinOutcome outcome, Action onComplete);

        void PlayBomb(Action onComplete);

        void ShowGameOver(int zoneReached, bool continueOffered, int continueCost);
        void HideGameOver();

        void ShowCashOut(IReadOnlyList<BankEntry> haul, int zonesCleared);
        void HideCashOut();

        void ShowGiveUpConfirm(int rewardsAtStake);
        void HideGiveUpConfirm();
    }
}
