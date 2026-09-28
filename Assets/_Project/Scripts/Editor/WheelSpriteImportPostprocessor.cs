using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Applies the sprite import conventions so a re-import can never drop a 9-slice border. Borders are
    /// import-time data, hence an AssetPostprocessor; preserveAspect is a component property, enforced by
    /// UIHygieneValidator rule 6.
    /// </summary>
    public sealed class WheelSpriteImportPostprocessor : AssetPostprocessor
    {
        private const string SPRITE_ROOT = "Assets/_Project/Art/Sprites/";
        private const float PIXELS_PER_UNIT = 100f;

        /// <summary>
        /// Border is (left, bottom, right, top). It must cover a frame's whole corner arc (12px and 24px for the
        /// "4px" and "12px" frames, 28px for the gradient one) or the arc stretches into blurred corners. Zone panels
        /// are vertical gradients, so they slice horizontally only.
        /// </summary>
        private static readonly Dictionary<string, Vector4> BordersByAssetName =
            new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase)
            {
                { "UI_button_orange_standard",   new Vector4(40, 30, 40, 30) },
                { "UI_button_grey_standard",     new Vector4(40, 30, 40, 30) },
                { "ui_card_frame_12px_neutral",  new Vector4(24, 24, 24, 24) },
                { "ui_card_frame_4px_zone",      new Vector4(12, 12, 12, 12) },
                { "ui_card_frame_gardient",      new Vector4(28, 28, 28, 28) },
                // Four L-shaped corner brackets, arms reaching to ~pixel 28 of 64 — an 8px border sliced
                // through the arms and smeared them across the stretched middle; 29 clears them entirely.
                { "ui_card_zone_map_frame",      new Vector4(29, 29, 29, 29) },
                { "ui_card_panel_zone_bg",            new Vector4(4, 0, 4, 0) },
                { "ui_card_panel_zone_current",       new Vector4(4, 0, 4, 0) },
                { "ui_card_panel_zone_current_white", new Vector4(4, 0, 4, 0) },
                { "ui_card_panel_zone_coming",        new Vector4(4, 0, 4, 0) },
                { "ui_card_panel_zone_super",         new Vector4(4, 0, 4, 0) },
                { "ui_card_panel_zone_white",         new Vector4(4, 0, 4, 0) },
            };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SPRITE_ROOT, StringComparison.Ordinal)) return;
            if (!(assetImporter is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PIXELS_PER_UNIT;
            importer.mipmapEnabled = false;          // UI is drawn 1:1; mips only cost memory and blur it
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            // FullRect rather than Tight: a tight mesh silently breaks 9-slicing and makes
            // preserveAspect layout unpredictable, and the overdraw saving is irrelevant here.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = BorderFor(assetPath);

            importer.SetTextureSettings(settings);
        }

        private static Vector4 BorderFor(string path)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            return BordersByAssetName.TryGetValue(name, out Vector4 border) ? border : Vector4.zero;
        }

        /// <summary>
        /// Re-applies the conventions to art that was imported before this postprocessor existed.
        /// Editing the border table alone does not re-import anything, so this is the way to roll a change out.
        /// </summary>
        [MenuItem("Tools/Vertigo/Reimport Sprite Conventions")]
        private static void ReimportAllSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SPRITE_ROOT.TrimEnd('/') });

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in guids)
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[Vertigo] Re-imported {guids.Length} sprites with the project import conventions.");
        }
    }
}
