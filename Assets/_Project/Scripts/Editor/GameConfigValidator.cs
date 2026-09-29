using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Reads the authored config assets back through the same code path the game uses and reports what it
    /// finds, so a wheel with a missing bomb or a catalog without its currencies is caught in the editor
    /// rather than on a play-through.
    /// </summary>
    public static class GameConfigValidator
    {
        private const string SETTINGS_FOLDER = "Assets/_Project/Configs/Settings";

        [MenuItem("Tools/Vertigo/Validate Game Configs")]
        public static void Validate()
        {
            var progression = AssetDatabase.LoadAssetAtPath<ZoneProgressionConfig>(
                $"{SETTINGS_FOLDER}/ZoneProgression_Default.asset");
            if (!progression)
            {
                Debug.LogError($"[Vertigo] No ZoneProgression_Default asset found in {SETTINGS_FOLDER}.");
                return;
            }

            var classifier = progression.CreateClassifier();
            var factory = new ZoneWheelFactory(classifier, progression, progression.Scaling);

            int problems = 0;
            int[] probeZones = { 1, 4, 5, 9, 10, 15, 19, 20, 25, 29, 30, 31, 35, 60, 61, 120 };

            foreach (int zone in probeZones)
            {
                try
                {
                    WheelModel wheel = factory.Build(zone);
                    var type = classifier.Classify(zone);

                    if (wheel.SliceCount != WheelModel.STANDARD_SLICE_COUNT)
                    {
                        Debug.LogError($"[Vertigo] Zone {zone}: {wheel.SliceCount} slices, expected {WheelModel.STANDARD_SLICE_COUNT}.");
                        problems++;
                    }

                    int expectedBombs = type == ZoneType.Normal ? 1 : 0;
                    if (wheel.BombCount != expectedBombs)
                    {
                        Debug.LogError($"[Vertigo] Zone {zone} ({type}): {wheel.BombCount} bomb(s), expected {expectedBombs}.");
                        problems++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Vertigo] Zone {zone} failed to build: {e.Message}");
                    problems++;
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RewardCatalog>($"{SETTINGS_FOLDER}/RewardCatalog.asset");
            if (!catalog)
            {
                Debug.LogError("[Vertigo] Reward catalog is missing.");
                problems++;
            }
            else
            {
                int withoutIcon = catalog.All.Count(r => !r || !r.Icon);
                if (withoutIcon > 0)
                {
                    Debug.LogWarning($"[Vertigo] {withoutIcon} catalog entr(ies) have no icon assigned.");
                }

                try
                {
                    // The revive currency must resolve to a catalog entry, or gold revives have nothing to charge.
                    if (!catalog.Find(catalog.GoldCurrency))
                    {
                        Debug.LogError("[Vertigo] The gold currency is not one of the catalog's own rewards.");
                        problems++;
                    }
                }
                catch (InvalidOperationException e)
                {
                    Debug.LogError($"[Vertigo] {e.Message} Cash-out could not convert it into the wallet.");
                    problems++;
                }
            }

            if (problems == 0)
                Debug.Log($"[Vertigo] Config validation passed across {probeZones.Length} probe zones.");
            else
                Debug.LogError($"[Vertigo] Config validation found {problems} problem(s).");
        }
    }
}
