using System;
using UnityEngine;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>The wheel a special zone type (safe, super) always spins, whatever depth it is reached at.</summary>
    [Serializable]
    public sealed class ZoneTypeWheel
    {
        [SerializeField] private ZoneType _zoneType;
        [SerializeField] private ZoneWheelConfig _wheel;

        public ZoneType ZoneType
        {
            get { return _zoneType; }
        }
        public ZoneWheelConfig Wheel
        {
            get { return _wheel; }
        }
    }
}
