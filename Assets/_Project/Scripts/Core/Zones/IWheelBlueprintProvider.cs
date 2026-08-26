using Vertigo.Wheel.Core.Spin;

namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>
    /// Supplies the authored wheel for a given zone.
    /// <para>
    /// This is the port that keeps <see cref="Spin.ZoneWheelFactory"/> free of Unity: in the player it is
    /// backed by the ZoneProgressionConfig asset (which resolves band overrides and the safe/super wheels),
    /// and in tests by a couple of lines of stub.
    /// </para>
    /// </summary>
    public interface IWheelBlueprintProvider
    {
        WheelBlueprint GetBlueprint(int zone, ZoneType zoneType);
    }
}
