using System;
using UnityEngine;

namespace Vertigo.Wheel.Data.Configs
{
    /// <summary>Swaps the whole normal-zone slice set once a depth is reached, tiering the reward pool.</summary>
    [Serializable]
    public sealed class ZoneBandOverride
    {
        [Min(1)] [SerializeField] private int _fromZone = 10;
        [SerializeField] private ZoneWheelConfig _wheel;

        public int FromZone => _fromZone;
        public ZoneWheelConfig Wheel => _wheel;
    }
}
