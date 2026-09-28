using System;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.States
{
    /// <summary>The zone strip and the wheel it spins.</summary>
    public interface IZonePresentation
    {
        /// <summary>Re-themes the wheel for a new zone and scrolls the zone map to it.</summary>
        void ShowZone(int zone, ZoneType zoneType, WheelModel wheel, Action onComplete);
    }
}
