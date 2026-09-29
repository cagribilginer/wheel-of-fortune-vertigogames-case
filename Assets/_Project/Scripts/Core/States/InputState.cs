namespace Vertigo.Wheel.Core.States
{
    /// <summary>Which buttons the player may press right now. Decided by the run, only mirrored by the screen.</summary>
    public readonly struct InputState
    {
        public static readonly InputState Locked = new(canSpin: false, canLeave: false);

        public readonly bool CanSpin;
        public readonly bool CanLeave;

        public InputState(bool canSpin, bool canLeave)
        {
            CanSpin = canSpin;
            CanLeave = canLeave;
        }
    }
}
