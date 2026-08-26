using System;

namespace Vertigo.Wheel.Core.Rewards
{
    /// <summary>
    /// Stable identity of a reward, decoupled from any Unity asset.
    /// <para>
    /// The core layer deals only in ids; turning an id back into a sprite is the presentation layer's job
    /// (via the RewardCatalog). That separation is what lets the entire game loop be tested without a scene.
    /// </para>
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
        public static RewardId None => default;

        public string Value => _value ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public bool Equals(RewardId other) => string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RewardId other && Equals(other);

        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        public static bool operator ==(RewardId left, RewardId right) => left.Equals(right);

        public static bool operator !=(RewardId left, RewardId right) => !left.Equals(right);

        public override string ToString() => IsEmpty ? "<none>" : _value;
    }
}
