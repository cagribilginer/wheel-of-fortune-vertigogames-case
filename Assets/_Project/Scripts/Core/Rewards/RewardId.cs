using System;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// Stable identity of a reward, decoupled from any Unity asset. The core deals only in ids; the
    /// RewardCatalog turns one back into a sprite, which keeps the whole game loop testable without a scene.
    /// </summary>
    public readonly struct RewardId : IEquatable<RewardId>
    {
        private readonly string _value;

        public RewardId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
                throw new ArgumentException("A RewardId cannot be the empty string; use RewardId.None.", nameof(value));

            _value = value;
        }

        /// <summary>The absent id, carried by bomb slices.</summary>
        public static RewardId None
        {
            get { return default; }
        }

        public string Value
        {
            get { return _value ?? string.Empty; }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(_value); }
        }

        public bool Equals(RewardId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RewardId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public static bool operator ==(RewardId left, RewardId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RewardId left, RewardId right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return IsEmpty ? "<none>" : _value;
        }
    }
}
