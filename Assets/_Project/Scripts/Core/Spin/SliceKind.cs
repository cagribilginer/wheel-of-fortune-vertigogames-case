namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>What a single wheel slice does when the indicator lands on it.</summary>
    public enum SliceKind
    {
        /// <summary>Grants its reward into the run bank.</summary>
        Reward = 0,

        /// <summary>Clears the entire run bank and ends the run.</summary>
        Bomb = 1
    }
}
