using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// One-time setup: marks the runtime config ScriptableObjects (previously loaded with
    /// <c>Resources.Load</c>) as Addressable, with an address matching their old Resources path 1:1, so
    /// <c>GameInstaller</c>'s <see cref="global::UnityEngine.AddressableAssets.Addressables"/> calls
    /// resolve with no further Groups-window setup. Run once via the menu item below, after the
    /// Addressables package has finished importing (check the Console is clear of compile errors first).
    /// Re-running is safe — it reuses each asset's existing entry rather than duplicating it.
    /// </summary>
    public static class AddressablesMigrationTool
    {
        private static readonly (string AssetPath, string Address)[] Entries =
        {
            ("Assets/Resources/Configs/Settings/RewardCatalog.asset", "Configs/Settings/RewardCatalog"),
            ("Assets/Resources/Configs/Settings/WheelSpin_Default.asset", "Configs/Settings/WheelSpin_Default"),
            ("Assets/Resources/Configs/Settings/ZoneProgression_Default.asset", "Configs/Settings/ZoneProgression_Default"),
            ("Assets/Resources/Configs/Settings/Continue_Default.asset", "Configs/Settings/Continue_Default"),
            ("Assets/Resources/Configs/Settings/AudioLibrary.asset", "Configs/Settings/AudioLibrary"),
            ("Assets/Resources/Configs/Settings/Juice_Default.asset", "Configs/Settings/Juice_Default"),
        };

        [MenuItem("Tools/Vertigo/Assets/Migrate Configs To Addressables")]
        public static void Migrate()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError(
                    "[Vertigo] AddressablesMigrationTool: could not create or find AddressableAssetSettings.");
                return;
            }

            AddressableAssetGroup group = settings.DefaultGroup;
            int migrated = 0;

            foreach ((string assetPath, string address) in Entries)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"[Vertigo] AddressablesMigrationTool: asset not found at {assetPath}.");
                    continue;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = address;
                migrated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[Vertigo] AddressablesMigrationTool: {migrated}/{Entries.Length} config asset(s) marked Addressable.");
        }
    }
}
