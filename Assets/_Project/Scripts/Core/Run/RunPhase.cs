namespace Vertigo.Wheel.Core.Run
{
    /// <summary>
    /// Coarse phase of the run, used for input legality decisions.
    /// Deliberately coarser than the state machine's state list: policies care about "is the wheel moving",
    /// not about which of two animation beats is currently playing.
    /// </summary>
    public enum RunPhase
    {
        /// <summary>Awaiting input. The only phase in which spinning or leaving is permitted.</summary>
        Idle = 0,

        /// <summary>The wheel is turning.</summary>
        Spinning = 1,

        /// <summary>The wheel has stopped; the result is being revealed and granted.</summary>
        Resolving = 2,

        /// <summary>A bomb ended the run; awaiting restart or continue.</summary>
        GameOver = 3,

        /// <summary>The player is walking away with their haul.</summary>
        CashOut = 4
    }
}
