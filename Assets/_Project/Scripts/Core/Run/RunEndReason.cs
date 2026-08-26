namespace Vertigo.Wheel.Core.Run
{
    /// <summary>Why a run stopped. Drives which popup opens and whether the haul was kept.</summary>
    public enum RunEndReason
    {
        /// <summary>Hit the bomb. The bank was cleared.</summary>
        Bomb = 0,

        /// <summary>Walked away from a safe or super zone. The haul was kept.</summary>
        CashedOut = 1,

        /// <summary>Abandoned voluntarily from a risky zone. The haul was forfeited.</summary>
        GaveUp = 2
    }
}
