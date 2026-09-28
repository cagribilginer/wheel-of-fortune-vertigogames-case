using System;

namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>
    /// Interval-based zone classification. Super is tested before Safe: zone 30 is both, and Super is a
    /// strict superset, so resolving the overlap that way costs the player nothing.
    /// </summary>
    public sealed class ZoneClassifier : IZoneClassifier
    {
        public const int DEFAULT_SAFE_INTERVAL = 5;
        public const int DEFAULT_SUPER_INTERVAL = 30;

        private readonly int _safeInterval;
        private readonly int _superInterval;

        public ZoneClassifier(int safeInterval = DEFAULT_SAFE_INTERVAL, int superInterval = DEFAULT_SUPER_INTERVAL)
        {
            if (safeInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(safeInterval), safeInterval, "Safe interval must be >= 1.");
            if (superInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(superInterval), superInterval, "Super interval must be >= 1.");

            _safeInterval = safeInterval;
            _superInterval = superInterval;
        }

        public int SafeInterval
        {
            get { return _safeInterval; }
        }
        public int SuperInterval
        {
            get { return _superInterval; }
        }

        /// <summary>
        /// True when every super zone is also a safe zone. When this does not hold, some super zones would
        /// not be reachable as safe zones and the progression reads inconsistently to a designer.
        /// </summary>
        public bool IntervalsAreConsistent
        {
            get { return _superInterval % _safeInterval == 0; }
        }

        public ZoneType Classify(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone), zone, "Zones are 1-indexed; the first zone is 1.");

            if (zone % _superInterval == 0) return ZoneType.Super;
            if (zone % _safeInterval == 0) return ZoneType.Safe;
            return ZoneType.Normal;
        }

        /// <summary>
        /// The first zone after <paramref name="fromZone"/> that <see cref="Classify"/> calls <paramref name="type"/>.
        /// Steps one zone at a time so a Safe search skips Super zones; bounded by one super plus one safe interval.
        /// </summary>
        public int NextZoneOfType(int fromZone, ZoneType type)
        {
            if (fromZone < 0) fromZone = 0;

            int guard = fromZone + _superInterval + _safeInterval + 1;
            for (int zone = fromZone + 1; zone <= guard; zone++)
                if (Classify(zone) == type)
                    return zone;

            return fromZone;
        }
    }
}
