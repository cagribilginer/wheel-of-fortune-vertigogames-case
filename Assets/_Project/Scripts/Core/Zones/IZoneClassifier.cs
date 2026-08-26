namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>Decides what kind of zone a given 1-indexed zone number is.</summary>
    public interface IZoneClassifier
    {
        ZoneType Classify(int zone);
    }
}
