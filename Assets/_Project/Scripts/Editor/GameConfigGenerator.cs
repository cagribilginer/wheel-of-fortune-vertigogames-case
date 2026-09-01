using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Builds the whole authored data set — rewards, themes, wheels, scaling, progression — in one pass.
    /// <para>
    /// Hand-authoring roughly thirty interlinked assets is slow and, worse, silently error-prone: a wheel
    /// missing its bomb or a catalog missing a reward only shows up on a play-through. Generating them
    /// makes the data set reproducible and reviewable as code.
    /// </para>
    /// <para>
    /// The generator is <b>idempotent</b>. Re-running it updates the existing assets in place rather than
    /// creating duplicates, so their GUIDs survive and any scene or prefab already referencing them keeps
    /// its wiring. That is what makes it safe to re-run after editing the tables below.
    /// </para>
    /// <para>
    /// Private <c>[SerializeField]</c> fields are written through <see cref="SerializedObject"/> rather than
    /// through public setters. That keeps the runtime API free of set-only properties that exist purely for
    /// tooling, and it is the only approach that works without weakening encapsulation.
    /// </para>
    /// </summary>
    public static class GameConfigGenerator
    {
        private const string ConfigRoot = "Assets/Resources/Configs";
        private const string SpriteRoot = EditorSpriteUtility.SpriteRoot;

        private const string RewardsFolder = ConfigRoot + "/Rewards";
        private const string ThemesFolder = ConfigRoot + "/Themes";
        private const string WheelsFolder = ConfigRoot + "/Wheels";
        private const string ScalingFolder = ConfigRoot + "/Scaling";
        private const string SettingsFolder = ConfigRoot + "/Settings";

        /// <summary>
        /// The currency the run bank converts into the persistent wallet on cash-out. The composition root
        /// must use this same id, so it lives in one place.
        /// </summary>
        public const string GoldRewardId = "Reward_Gold";

        // ------------------------------------------------------------------ authoring tables

        private readonly struct RewardSpec
        {
            public readonly string AssetName;
            public readonly string DisplayName;
            public readonly string SpriteName;
            public readonly RewardCategory Category;
            public readonly int BaseAmount;
            public readonly int UnitValue;

            public RewardSpec(string assetName, string displayName, string spriteName,
                RewardCategory category, int baseAmount, int unitValue)
            {
                AssetName = assetName;
                DisplayName = displayName;
                SpriteName = spriteName;
                Category = category;
                BaseAmount = baseAmount;
                UnitValue = unitValue;
            }
        }

        private static readonly RewardSpec[] Rewards =
        {
            // --- band 1 pool: zones 1-9 -------------------------------------------------
            new RewardSpec("Reward_PistolPoints",     "Pistol Points",    "UI_Icons_Pistol_Points",      RewardCategory.Points,     10, 1),
            new RewardSpec("Reward_KnifePoints",      "Knife Points",     "UI_Icons_Knife_Points",       RewardCategory.Points,      8, 1),
            new RewardSpec("Reward_ArmorPoints",      "Armor Points",     "UI_Icons_Armor_Points",       RewardCategory.Points,     12, 1),
            new RewardSpec("Reward_VestPoints",       "Vest Points",      "UI_Icons_Vest_Points",        RewardCategory.Points,     12, 1),
            new RewardSpec("Reward_ShotgunPoints",    "Shotgun Points",   "UI_Icons_Shotgun_Points",     RewardCategory.Points,     14, 1),
            new RewardSpec("Reward_Tier1Shotgun",     "Shotgun",          "UI_Icon_Renders_tier1_shotgun", RewardCategory.Weapon,    1, 25),
            new RewardSpec("Reward_Cash",             "Cash",             "UI_icon_cash",                RewardCategory.Currency,   50, 1),

            // --- band 2 pool: zones 10-19 -----------------------------------------------
            new RewardSpec("Reward_SmgPoints",        "SMG Points",       "UI_Icons_SMG_Points",         RewardCategory.Points,     20, 1),
            new RewardSpec("Reward_RiflePoints",      "Rifle Points",     "UI_Icons_Rifle_Points",       RewardCategory.Points,     20, 1),
            new RewardSpec("Reward_Tier2Rifle",       "Assault Rifle",    "UI_Icon_Renders_tier2_rifle", RewardCategory.Weapon,      1, 40),
            new RewardSpec("Reward_Tier2Mle",         "Melee Weapon",     "UI_Icon_Renders_tier2_mle",   RewardCategory.Weapon,      1, 40),
            new RewardSpec("Reward_GrenadeM67",       "M67 Grenade",      "ui_icon_render_cons_grenade_m67", RewardCategory.Consumable, 3, 8),
            new RewardSpec("Reward_Healthshot",       "Regenerator",      "ui_icon_render_cons_healthshot_2_regenerator", RewardCategory.Consumable, 3, 8),

            // --- band 3 pool: zones 20+ -------------------------------------------------
            new RewardSpec("Reward_SniperPoints",     "Sniper Points",    "UI_Icons_Sniper_Points",      RewardCategory.Points,     30, 2),
            new RewardSpec("Reward_SubmachinePoints", "Submachine Points","UI_Icons_Submachine_Points",  RewardCategory.Points,     30, 2),
            new RewardSpec("Reward_Tier3Sniper",      "Sniper Rifle",     "UI_Icon_Renders_tier3_sniper", RewardCategory.Weapon,     1, 80),
            new RewardSpec("Reward_Tier3Smg",         "Submachine Gun",   "UI_Icon_Renders_tier3_smg",   RewardCategory.Weapon,      1, 80),
            new RewardSpec("Reward_Molotov",          "Molotov",          "ui_icon_render_t_cons_molotov", RewardCategory.Consumable, 4, 10),
            new RewardSpec(GoldRewardId,              "Gold",             "UI_icon_gold",                RewardCategory.Currency,   40, 3),

            // --- safe zone extras -------------------------------------------------------
            new RewardSpec("Reward_ChestSilver",      "Silver Chest",     "UI_icon_chest_silver_nolight", RewardCategory.Chest,      1, 60),
            new RewardSpec("Reward_ChestStandard",    "Chest",            "UI_icon_chest_standart_nolight", RewardCategory.Chest,    1, 45),

            // --- super zone pool --------------------------------------------------------
            new RewardSpec("Reward_ChestSuper",       "Super Chest",      "UI_icon_chest_super_nolight", RewardCategory.Chest,       1, 200),
            new RewardSpec("Reward_ChestGold",        "Gold Chest",       "UI_icon_chest_gold_nolight",  RewardCategory.Chest,       1, 150),
            new RewardSpec("Reward_ChestBig",         "Big Chest",        "UI_icon_chest_big_nolight",   RewardCategory.Chest,       1, 120),
            new RewardSpec("Reward_BayonetSummer",    "Summer Bayonet",   "ui_icon_mle_bayonet_summer_vice", RewardCategory.Cosmetic, 1, 150),
            new RewardSpec("Reward_BayonetEaster",    "Easter Bayonet",   "ui_icon_mle_bayonet_easter_time", RewardCategory.Cosmetic, 1, 150),
            new RewardSpec("Reward_AviatorGlasses",   "Aviator Glasses",  "ui_icon_aviator_glasses_easter", RewardCategory.Cosmetic,  1, 100),
            new RewardSpec("Reward_PumpkinHelmet",    "Pumpkin Helmet",   "ui_icon_helmet_pumpkin",      RewardCategory.Cosmetic,    1, 100),
        };

        /// <summary>
        /// Seven rewards per bronze wheel, because the eighth slot is always the bomb.
        /// Safe and super wheels list eight, because they carry no bomb at all.
        /// </summary>
        private static readonly string[] Band1Pool =
        {
            "Reward_PistolPoints", "Reward_KnifePoints", "Reward_ArmorPoints", "Reward_VestPoints",
            "Reward_ShotgunPoints", "Reward_Tier1Shotgun", "Reward_Cash",
        };

        private static readonly string[] Band2Pool =
        {
            "Reward_SmgPoints", "Reward_RiflePoints", "Reward_Tier2Rifle", "Reward_Tier2Mle",
            "Reward_GrenadeM67", "Reward_Healthshot", "Reward_Cash",
        };

        private static readonly string[] Band3Pool =
        {
            "Reward_SniperPoints", "Reward_SubmachinePoints", "Reward_Tier3Sniper", "Reward_Tier3Smg",
            "Reward_Molotov", GoldRewardId, "Reward_Cash",
        };

        private static readonly string[] SafePool =
        {
            "Reward_ChestSilver", "Reward_ChestStandard", "Reward_SniperPoints", "Reward_SubmachinePoints",
            "Reward_Tier3Smg", "Reward_Molotov", GoldRewardId, "Reward_Cash",
        };

        private static readonly string[] SuperPool =
        {
            "Reward_ChestSuper", "Reward_ChestGold", "Reward_ChestBig", "Reward_BayonetSummer",
            "Reward_BayonetEaster", "Reward_AviatorGlasses", "Reward_PumpkinHelmet", GoldRewardId,
        };

        // ------------------------------------------------------------------ entry point

        [MenuItem("Tools/Vertigo/Generate Game Configs")]
        public static void Generate()
        {
            int created = 0;
            int updated = 0;

            // Folders must exist before the batch starts: AssetDatabase.IsValidFolder does not observe
            // folders created inside StartAssetEditing, so nesting these would misreport and CreateAsset
            // would then fail on a path that has no parent.
            EnsureFolders();

            try
            {
                AssetDatabase.StartAssetEditing();

                Dictionary<string, RewardDefinition> rewards = GenerateRewards(ref created, ref updated);
                Dictionary<WheelTier, WheelThemeConfig> themes = GenerateThemes(ref created, ref updated);
                LinearScalingSO scaling = GenerateScaling(ref created, ref updated);

                ZoneWheelConfig band1 = GenerateWheel("Wheel_Bronze_Band1", WheelTier.Bronze, themes[WheelTier.Bronze], Band1Pool, rewards, withBomb: true, ref created, ref updated);
                ZoneWheelConfig band2 = GenerateWheel("Wheel_Bronze_Band2", WheelTier.Bronze, themes[WheelTier.Bronze], Band2Pool, rewards, withBomb: true, ref created, ref updated);
                ZoneWheelConfig band3 = GenerateWheel("Wheel_Bronze_Band3", WheelTier.Bronze, themes[WheelTier.Bronze], Band3Pool, rewards, withBomb: true, ref created, ref updated);
                ZoneWheelConfig safe = GenerateWheel("Wheel_Silver_Safe", WheelTier.Silver, themes[WheelTier.Silver], SafePool, rewards, withBomb: false, ref created, ref updated);
                ZoneWheelConfig super = GenerateWheel("Wheel_Golden_Super", WheelTier.Golden, themes[WheelTier.Golden], SuperPool, rewards, withBomb: false, ref created, ref updated);

                GenerateProgression(band1, band2, band3, safe, super, scaling, ref created, ref updated);
                GenerateSpinConfig(ref created, ref updated);
                GenerateContinueConfig(ref created, ref updated);
                GenerateCatalog(rewards, ref created, ref updated);
                GenerateAudioLibrary(ref created, ref updated);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(
                $"[Vertigo] Game configs generated in {ConfigRoot}: {created} created, {updated} updated. " +
                $"Re-running is safe — existing assets keep their GUIDs, so scene references survive.");

            ZoneProgressionConfig progression = Load<ZoneProgressionConfig>($"{SettingsFolder}/ZoneProgression_Default.asset");
            if (progression != null) Selection.activeObject = progression;
        }

        // ------------------------------------------------------------------ generators

        private static Dictionary<string, RewardDefinition> GenerateRewards(ref int created, ref int updated)
        {
            var byName = new Dictionary<string, RewardDefinition>(Rewards.Length);
            var missingSprites = new List<string>();

            foreach (RewardSpec spec in Rewards)
            {
                RewardDefinition asset = LoadOrCreate<RewardDefinition>(
                    $"{RewardsFolder}/{spec.AssetName}.asset", ref created, ref updated);

                Sprite icon = EditorSpriteUtility.FindSprite(spec.SpriteName);
                if (icon == null) missingSprites.Add(spec.SpriteName);

                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = spec.AssetName;
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_icon").objectReferenceValue = icon;
                so.FindProperty("_category").enumValueIndex = (int)spec.Category;
                so.FindProperty("_defaultBaseAmount").intValue = spec.BaseAmount;
                so.FindProperty("_estimatedValue").intValue = spec.UnitValue;
                so.ApplyModifiedPropertiesWithoutUndo();

                byName[spec.AssetName] = asset;
            }

            if (missingSprites.Count > 0)
            {
                Debug.LogWarning(
                    $"[Vertigo] {missingSprites.Count} reward sprite(s) not found under {SpriteRoot} and left " +
                    $"unassigned: {string.Join(", ", missingSprites)}");
            }

            return byName;
        }

        private static Dictionary<WheelTier, WheelThemeConfig> GenerateThemes(ref int created, ref int updated)
        {
            var themes = new Dictionary<WheelTier, WheelThemeConfig>(3);

            AddTheme(WheelTier.Bronze, "Theme_Bronze", "ui_spin_bronze_base", "ui_spin_bronze_indicator",
                new Color(0.80f, 0.49f, 0.20f), themes, ref created, ref updated);
            AddTheme(WheelTier.Silver, "Theme_Silver", "ui_spin_silver_base", "ui_spin_silver_indicator",
                new Color(0.78f, 0.82f, 0.86f), themes, ref created, ref updated);
            AddTheme(WheelTier.Golden, "Theme_Golden", "ui_spin_golden_base", "ui_spin_golden_indicator",
                new Color(0.95f, 0.76f, 0.20f), themes, ref created, ref updated);

            return themes;
        }

        private static void AddTheme(
            WheelTier tier, string assetName, string baseSprite, string indicatorSprite, Color accent,
            Dictionary<WheelTier, WheelThemeConfig> themes, ref int created, ref int updated)
        {
            WheelThemeConfig asset = LoadOrCreate<WheelThemeConfig>(
                $"{ThemesFolder}/{assetName}.asset", ref created, ref updated);

            var so = new SerializedObject(asset);
            so.FindProperty("_baseSprite").objectReferenceValue = EditorSpriteUtility.FindSprite(baseSprite);
            so.FindProperty("_indicatorSprite").objectReferenceValue = EditorSpriteUtility.FindSprite(indicatorSprite);
            so.FindProperty("_accentColor").colorValue = accent;
            so.FindProperty("_glowColor").colorValue = new Color(accent.r, accent.g, accent.b, 0.65f);
            so.ApplyModifiedPropertiesWithoutUndo();

            themes[tier] = asset;
        }

        private static LinearScalingSO GenerateScaling(ref int created, ref int updated)
        {
            LinearScalingSO asset = LoadOrCreate<LinearScalingSO>(
                $"{ScalingFolder}/Scaling_Linear_Default.asset", ref created, ref updated);

            var so = new SerializedObject(asset);
            so.FindProperty("_growthPerZone").floatValue = 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return asset;
        }

        private static ZoneWheelConfig GenerateWheel(
            string assetName, WheelTier tier, WheelThemeConfig theme, IReadOnlyList<string> pool,
            IReadOnlyDictionary<string, RewardDefinition> rewards, bool withBomb,
            ref int created, ref int updated)
        {
            ZoneWheelConfig asset = LoadOrCreate<ZoneWheelConfig>(
                $"{WheelsFolder}/{assetName}.asset", ref created, ref updated);

            int expectedRewards = withBomb ? WheelModel.StandardSliceCount - 1 : WheelModel.StandardSliceCount;
            if (pool.Count != expectedRewards)
            {
                Debug.LogError(
                    $"[Vertigo] Wheel '{assetName}' expects {expectedRewards} rewards but its pool lists " +
                    $"{pool.Count}. The wheel was still written; fix the pool table and re-run.");
            }

            var so = new SerializedObject(asset);
            so.FindProperty("_tier").enumValueIndex = (int)tier;
            so.FindProperty("_theme").objectReferenceValue = theme;
            // On: the slice list stays a readable picture of the pool, but the factory deals it onto
            // different wedges each zone so rewards aren't visually pinned to the same wedge every time.
            so.FindProperty("_shuffleSliceOrder").boolValue = true;

            SerializedProperty slices = so.FindProperty("_slices");
            slices.arraySize = WheelModel.StandardSliceCount;

            for (int i = 0; i < WheelModel.StandardSliceCount; i++)
            {
                SerializedProperty element = slices.GetArrayElementAtIndex(i);

                // Slot 0 carries the bomb on risky wheels. A fixed slot is not exploitable, because the
                // wheel spins a random number of whole turns before it lands — and keeping it fixed makes
                // the inspector list a literal picture of the wheel.
                bool isBomb = withBomb && i == 0;

                element.FindPropertyRelative("_kind").enumValueIndex = (int)(isBomb ? SliceKind.Bomb : SliceKind.Reward);
                element.FindPropertyRelative("_weight").intValue = 1;
                element.FindPropertyRelative("_baseAmountOverride").intValue = 0;

                RewardDefinition reward = null;
                if (!isBomb)
                {
                    int poolIndex = withBomb ? i - 1 : i;
                    if (poolIndex >= 0 && poolIndex < pool.Count) rewards.TryGetValue(pool[poolIndex], out reward);
                }

                element.FindPropertyRelative("_reward").objectReferenceValue = reward;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static void GenerateProgression(
            ZoneWheelConfig band1, ZoneWheelConfig band2, ZoneWheelConfig band3,
            ZoneWheelConfig safe, ZoneWheelConfig super, ScalingStrategySO scaling,
            ref int created, ref int updated)
        {
            ZoneProgressionConfig asset = LoadOrCreate<ZoneProgressionConfig>(
                $"{SettingsFolder}/ZoneProgression_Default.asset", ref created, ref updated);

            var so = new SerializedObject(asset);
            so.FindProperty("_safeZoneInterval").intValue = 5;
            so.FindProperty("_superZoneInterval").intValue = 30;
            so.FindProperty("_defaultNormalWheel").objectReferenceValue = band1;
            so.FindProperty("_safeWheel").objectReferenceValue = safe;
            so.FindProperty("_superWheel").objectReferenceValue = super;
            so.FindProperty("_scaling").objectReferenceValue = scaling;

            // 0 means endless, which is what ships. The demo cap is a recording aid only.
            so.FindProperty("_demoMaxZone").intValue = 0;

            SerializedProperty bands = so.FindProperty("_bandOverrides");
            bands.arraySize = 2;
            SetBand(bands.GetArrayElementAtIndex(0), 10, band2);
            SetBand(bands.GetArrayElementAtIndex(1), 20, band3);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBand(SerializedProperty element, int fromZone, ZoneWheelConfig wheel)
        {
            element.FindPropertyRelative("_fromZone").intValue = fromZone;
            element.FindPropertyRelative("_wheel").objectReferenceValue = wheel;
        }

        private static void GenerateSpinConfig(ref int created, ref int updated)
        {
            WheelSpinConfig asset = LoadOrCreate<WheelSpinConfig>(
                $"{SettingsFolder}/WheelSpin_Default.asset", ref created, ref updated);

            var so = new SerializedObject(asset);
            so.FindProperty("_duration").floatValue = 3.2f;
            so.FindProperty("_minTurns").intValue = 4;
            so.FindProperty("_maxTurns").intValue = 6;
            so.FindProperty("_settlePunchDegrees").floatValue = 2.5f;
            so.FindProperty("_tickPunchDegrees").floatValue = 10f;
            so.FindProperty("_revealDelay").floatValue = 0.35f;

            // A steep ramp, a long glide, then a hard stop. An Ease enum decelerates too gently and the
            // final third of the spin reads as dead time.
            so.FindProperty("_spinEase").animationCurveValue = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.25f, 0.35f, 2f, 2f),
                new Keyframe(0.80f, 0.93f, 0.5f, 0.5f),
                new Keyframe(1f, 1f, 0f, 0f));

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GenerateContinueConfig(ref int created, ref int updated)
        {
            ContinueConfig asset = LoadOrCreate<ContinueConfig>(
                $"{SettingsFolder}/Continue_Default.asset", ref created, ref updated);

            var so = new SerializedObject(asset);
            so.FindProperty("_baseCost").intValue = 50;
            so.FindProperty("_costPerZone").intValue = 10;
            so.FindProperty("_maxAdRevivesPerRun").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GenerateCatalog(
            IReadOnlyDictionary<string, RewardDefinition> rewards, ref int created, ref int updated)
        {
            RewardCatalog asset = LoadOrCreate<RewardCatalog>(
                $"{SettingsFolder}/RewardCatalog.asset", ref created, ref updated);

            var so = new SerializedObject(asset);
            SerializedProperty all = so.FindProperty("_all");

            // Authoring-table order, not dictionary order, so the asset diffs stably between runs.
            all.arraySize = Rewards.Length;
            for (int i = 0; i < Rewards.Length; i++)
            {
                rewards.TryGetValue(Rewards[i].AssetName, out RewardDefinition definition);
                all.GetArrayElementAtIndex(i).objectReferenceValue = definition;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Just the drop target — no <c>demo_content</c> clips exist to assign, so this only ensures the
        /// asset <c>GameInstaller</c>'s <c>Resources.Load</c> expects actually exists. Whoever sources SFX
        /// later drags clips onto this same asset; nothing about the loading path changes.
        /// </summary>
        private static void GenerateAudioLibrary(ref int created, ref int updated) =>
            LoadOrCreate<AudioLibrary>($"{SettingsFolder}/AudioLibrary.asset", ref created, ref updated);

        // ------------------------------------------------------------------ helpers

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ConfigRoot);
            EnsureFolder(RewardsFolder);
            EnsureFolder(ThemesFolder);
            EnsureFolder(WheelsFolder);
            EnsureFolder(ScalingFolder);
            EnsureFolder(SettingsFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// Loads the asset if it already exists so its GUID — and therefore every reference to it — survives
        /// a re-run. Only genuinely missing assets are created.
        /// </summary>
        private static T LoadOrCreate<T>(string path, ref int created, ref int updated)
            where T : ScriptableObject
        {
            T existing = Load<T>(path);
            if (existing != null)
            {
                updated++;
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created++;
            return asset;
        }

        private static T Load<T>(string path) where T : ScriptableObject =>
            AssetDatabase.LoadAssetAtPath<T>(path);

        /// <summary>
        /// Reads the generated data back through the same code path the game uses and reports what it found.
        /// A generator that silently produced a wheel with two bombs would otherwise only be caught in play.
        /// </summary>
        [MenuItem("Tools/Vertigo/Validate Game Configs")]
        public static void Validate()
        {
            var progression = Load<ZoneProgressionConfig>($"{SettingsFolder}/ZoneProgression_Default.asset");
            if (progression == null)
            {
                Debug.LogError("[Vertigo] No progression asset found. Run Tools/Vertigo/Generate Game Configs first.");
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

                    if (wheel.SliceCount != WheelModel.StandardSliceCount)
                    {
                        Debug.LogError($"[Vertigo] Zone {zone}: {wheel.SliceCount} slices, expected {WheelModel.StandardSliceCount}.");
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

            var catalog = Load<RewardCatalog>($"{SettingsFolder}/RewardCatalog.asset");
            if (catalog == null)
            {
                Debug.LogError("[Vertigo] Reward catalog is missing.");
                problems++;
            }
            else
            {
                int withoutIcon = catalog.All.Count(r => r == null || r.Icon == null);
                if (withoutIcon > 0)
                {
                    Debug.LogWarning($"[Vertigo] {withoutIcon} catalog entr(ies) have no icon assigned.");
                }

                if (catalog.Find(GoldRewardId) == null)
                {
                    Debug.LogError(
                        $"[Vertigo] The catalog has no '{GoldRewardId}'. Cash-out could not convert gold " +
                        "into the persistent wallet.");
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
