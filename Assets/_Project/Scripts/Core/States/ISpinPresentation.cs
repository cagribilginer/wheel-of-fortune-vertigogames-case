using System;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>The spin and what it lands on.</summary>
    public interface ISpinPresentation
    {
        /// <summary>Rotates the wheel to a slot the logic has already committed to.</summary>
        void PlaySpin(int slotIndex, Action onComplete);

        /// <summary>The landing beat; <paramref name="zoneType"/> lets the screen flag a safe or super clear.</summary>
        void PlayReveal(SpinOutcome outcome, ZoneType zoneType, Action onComplete);

        void PlayRewardGranted(SpinOutcome outcome, Action onComplete);

        void PlayBomb(Action onComplete);
    }
}
