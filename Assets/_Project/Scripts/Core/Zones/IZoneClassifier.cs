namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>Decides what kind of zone a given 1-indexed zone number is.</summary>
    public interface IZoneClassifier
    {
        ZoneType Classify(int zone);

        /// <summary>
        /// The first zone strictly after <paramref name="fromZone"/> that classifies as
        /// <paramref name="type"/>. The "next safe / next super" milestone badges need this rather than a
        /// raw interval multiple: zone 30 is Super, not a regular Safe zone, so the next Safe after 26 is 35.
        /// </summary>
        int NextZoneOfType(int fromZone, ZoneType type);
    }
}
