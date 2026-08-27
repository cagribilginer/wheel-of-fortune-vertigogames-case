using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Applies the project's sprite import conventions automatically, so nobody has to remember them and
    /// a re-import can never silently drop a 9-slice border.
    /// <para>
    /// The case requires Sliced sprites in Image components. A slice border lives in the <em>texture
    /// importer</em>, not on the Image, so it is import-time data — which makes an AssetPostprocessor the
    /// right owner rather than a checklist item. The border sizes for the frame assets are published in
    /// their own filenames (<c>ui_card_frame_12px_neutral</c>), so the table below is transcription, not
    /// invention.
    /// </para>
    /// <para>
    /// Note the division of labour: this sets <em>import</em> state (type, mesh, mipmaps, borders).
    /// <c>Image.preserveAspect</c> is a component property, not import data, so it is enforced by
    /// UIHygieneValidator rule 6 instead.
    /// </para>
    /// </summary>
    public sealed class WheelSpriteImportPostprocessor : AssetPostprocessor
    {
        private const string SpriteRoot = "Assets/_Project/Art/Sprites/";
        private const float PixelsPerUnit = 100f;

        /// <summary>
        /// Border is (left, bottom, right, top).
        /// <para>
        /// The zone panels are 64x64 <em>vertical gradients</em>: colour is constant along X, so stretching
        /// horizontally is lossless, while a vertical 9-slice would repeat the middle row and flatten the
        /// ramp. Hence horizontal-only borders — genuinely Sliced, without damaging the art.
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, Vector4> BordersByAssetName =
            new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase)
            {
                { "UI_button_orange_standard",   new Vector4(40, 30, 40, 30) },
                { "UI_button_grey_standard",     new Vector4(40, 30, 40, 30) },
                { "ui_card_frame_12px_neutral",  new Vector4(12, 12, 12, 12) },
                { "ui_card_frame_4px_zone",      new Vector4(4, 4, 4, 4) },
                { "ui_card_frame_gardient",      new Vector4(12, 12, 12, 12) },
                // Not a continuous rectangular outline — the art is four independent L-shaped corner
                // brackets whose arms reach to about pixel 28 of 64. An 8px border sliced straight through
                // those arms, so the "stretched middle" strip pulled in part of the bracket itself and
                // smeared it across the whole panel width. 29 clears the arms entirely, leaving a genuinely
                // transparent 6px band as the only thing that ever gets stretched.
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
            if (!assetPath.StartsWith(SpriteRoot, StringComparison.Ordinal)) return;
            if (!(assetImporter is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
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
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteRoot.TrimEnd('/') });

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
