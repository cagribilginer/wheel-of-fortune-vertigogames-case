namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>Classification of a single zone, which decides the wheel tier and whether a bomb is present.</summary>
    public enum ZoneType
    {
        /// <summary>Risky zone: the wheel carries exactly one bomb slice and leaving is not permitted.</summary>
        Normal = 0,

        /// <summary>Every Nth zone (default 5): silver, bomb-free, and the player may walk away.</summary>
        Safe = 1,

        /// <summary>Every Mth zone (default 30): golden, bomb-free, special rewards, and the player may walk away.</summary>
        Super = 2
    }
}
