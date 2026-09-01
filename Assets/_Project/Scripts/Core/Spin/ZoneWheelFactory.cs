using System;
using System.Collections.Generic;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Zones;

namespace Vertigo.Wheel.Core.Spin
{
    /// <summary>
    /// Builds the wheel for a zone: classify the zone, fetch the authored blueprint for it, then scale
    /// every slice's amount to that depth.
    /// <para>
    /// Assembling a wheel is neither the state machine's job nor the config asset's, so it gets its own
    /// class with exactly one reason to change.
    /// </para>
    /// </summary>
    public sealed class ZoneWheelFactory
    {
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

            // A safe or super zone that still carried a bomb would silently break the headline promise of
            // the whole mode, so it is a hard failure rather than something to notice on a play-through.
            if (zoneType != ZoneType.Normal && blueprint.BombCount > 0)
                throw new InvalidOperationException(
                    $"Zone {zone} is {zoneType} and must be risk-free, but its wheel carries {blueprint.BombCount} bomb slice(s).");

            IReadOnlyList<SliceBlueprint> authored = blueprint.Slices;
            var slices = new List<WheelSlice>(authored.Count);

            for (int i = 0; i < authored.Count; i++)
                slices.Add(authored[i].ToSlice(zone, _scaling));

            if (blueprint.ShuffleSlices && _random != null)
                Shuffle(slices);

            return new WheelModel(blueprint.Tier, slices);
        }

        // Fisher-Yates over the materialised slices: same pool, new wedge order for this zone. Weight
        // travels with each slice, so weighted resolution is unaffected; bomb count is unchanged, so the
        // safe/super rule checked above still holds.
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
