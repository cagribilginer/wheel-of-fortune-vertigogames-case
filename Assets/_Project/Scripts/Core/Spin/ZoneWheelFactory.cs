using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// Builds the wheel for a zone: classify it, fetch the authored blueprint, scale every slice to that depth.
    /// Its own class so it has one reason to change.
    /// </summary>
    public sealed class ZoneWheelFactory
    {
        public const int NORMAL_ZONE_BOMB_COUNT = 1;

        private readonly IZoneClassifier _classifier;
        private readonly IWheelBlueprintProvider _blueprints;
        private readonly IRewardScaling _scaling;
        private readonly IRandomProvider _random;

        public ZoneWheelFactory(
            IZoneClassifier classifier,
            IWheelBlueprintProvider blueprints,
            IRewardScaling scaling,
            IRandomProvider random = null)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            _scaling = scaling ?? throw new ArgumentNullException(nameof(scaling));

            // Optional: a blueprint that opts into shuffling only actually shuffles when the factory was
            // given a randomness source. Tests leave it null, so their wheels stay in blueprint order.
            _random = random;
        }

        public WheelModel Build(int zone)
        {
            ZoneType zoneType = _classifier.Classify(zone);
            return Build(zone, zoneType);
        }

        public WheelModel Build(int zone, ZoneType zoneType)
        {
            WheelBlueprint blueprint = _blueprints.GetBlueprint(zone, zoneType);

            if (blueprint == null)
                throw new InvalidOperationException(
                    $"No wheel blueprint was configured for zone {zone} ({zoneType}).");

            // The mode's headline promise: a normal zone carries exactly one bomb, a safe or super zone none.
            // A wheel that misses the count is a hard failure here, not something to notice in play.
            int expectedBombs = zoneType == ZoneType.Normal ? NORMAL_ZONE_BOMB_COUNT : 0;
            if (blueprint.BombCount != expectedBombs)
                throw new InvalidOperationException(
                    $"Zone {zone} is {zoneType} and must carry exactly {expectedBombs} bomb slice(s), " +
                    $"but its wheel carries {blueprint.BombCount}.");

            IReadOnlyList<SliceBlueprint> authored = blueprint.Slices;
            var slices = new List<WheelSlice>(authored.Count);

            for (int i = 0; i < authored.Count; i++)
                slices.Add(authored[i].ToSlice(zone, _scaling));

            if (blueprint.IsShuffleEnabled && _random != null)
                Shuffle(slices);

            return new WheelModel(blueprint.Tier, slices);
        }

        // Fisher-Yates over the slices: weight travels with each slice and the bomb count is unchanged.
        private void Shuffle(List<WheelSlice> slices)
        {
            for (int i = slices.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (slices[i], slices[j]) = (slices[j], slices[i]);
            }
        }
    }
}
