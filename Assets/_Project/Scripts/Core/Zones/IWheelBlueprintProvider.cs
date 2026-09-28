using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>
    /// Supplies the authored wheel for a zone. The port that keeps ZoneWheelFactory free of Unity:
    /// ZoneProgressionConfig backs it in the player, a stub in tests.
    /// </summary>
    public interface IWheelBlueprintProvider
    {
        WheelBlueprint GetBlueprint(int zone, ZoneType zoneType);
    }
}
