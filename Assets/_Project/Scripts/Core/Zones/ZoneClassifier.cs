using System;

namespace Vertigo.Wheel.Core.Zones
{
    /// <summary>
    /// Interval-based zone classification.
    /// <para>
    /// The super interval is tested <em>before</em> the safe interval. Zone 30 is both a 5th and a 30th
    /// zone, and Super is a strict superset of Safe (bomb-free, leaving allowed, better rewards), so
    /// resolving the overlap in favour of Super costs the player nothing and gains them the special pool.
    /// </para>
    /// </summary>
    public sealed class ZoneClassifier : IZoneClassifier
    {
        public const int DefaultSafeInterval = 5;
        public const int DefaultSuperInterval = 30;

        private readonly int _safeInterval;
        private readonly int _superInterval;

        public ZoneClassifier(int safeInterval = DefaultSafeInterval, int superInterval = DefaultSuperInterval)
        {
            if (safeInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(safeInterval), safeInterval, "Safe interval must be >= 1.");
            if (superInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(superInterval), superInterval, "Super interval must be >= 1.");

            _safeInterval = safeInterval;
            _superInterval = superInterval;
        }

        public int SafeInterval => _safeInterval;
        public int SuperInterval => _superInterval;

        /// <summary>
        /// True when every super zone is also a safe zone. When this does not hold, some super zones would
        /// not be reachable as safe zones and the progression reads inconsistently to a designer.
        /// </summary>
        public bool IntervalsAreConsistent => _superInterval % _safeInterval == 0;

        public ZoneType Classify(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone), zone, "Zones are 1-indexed; the first zone is 1.");

            // The opening zone is always safe: the run should never be able to end on the very first spin,
            // and the player needs one bomb-free zone to bank something before any risk is on the table.
            if (zone == 1) return ZoneType.Safe;

            if (zone % _superInterval == 0) return ZoneType.Super;
            if (zone % _safeInterval == 0) return ZoneType.Safe;
            return ZoneType.Normal;
        }
    }
}
