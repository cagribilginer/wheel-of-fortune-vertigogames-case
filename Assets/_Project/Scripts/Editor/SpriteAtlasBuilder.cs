using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Packs the UI icon and chrome sprites into two <see cref="SpriteAtlas"/> assets so each screen
    /// draws from one texture instead of paying a draw call per icon. Run once via the menu item below;
    /// re-running is safe — it clears each atlas's packed folders and re-adds the current ones, so it
    /// always reflects whatever is on disk right now rather than accumulating stale entries.
    /// </summary>
    public static class SpriteAtlasBuilder
    {
        private const string SPRITES_ROOT = "Assets/_Project/Art/Sprites";
        private const string ICON_ATLAS_PATH = SPRITES_ROOT + "/Atlas_Icons.spriteatlas";
        private const string UI_CHROME_ATLAS_PATH = SPRITES_ROOT + "/Atlas_UIChrome.spriteatlas";

        // Reward/currency icons: small, numerous (~36), the ones the report calls out as one-draw-call-each.
        private static readonly string[] IconFolders =
        {
            SPRITES_ROOT + "/Icons/Rewards",
            SPRITES_ROOT + "/Icons/Currency",
            SPRITES_ROOT + "/Icons/Misc",
            SPRITES_ROOT + "/Icons/UI",
        };

        // Panel/frame/button chrome: a second, separate atlas since these differ enough in size and reuse
        // pattern from the icons that packing them together would just waste atlas padding.
        private static readonly string[] UIChromeFolders =
        {
            SPRITES_ROOT + "/Frames",
            SPRITES_ROOT + "/Panels",
            SPRITES_ROOT + "/Buttons",
        };

        [MenuItem("Tools/Vertigo/Assets/Build Sprite Atlases")]
        public static void Build()
        {
            SpriteAtlas iconAtlas = BuildAtlas(ICON_ATLAS_PATH, IconFolders);
            SpriteAtlas uiAtlas = BuildAtlas(UI_CHROME_ATLAS_PATH, UIChromeFolders);

            SpriteAtlasUtility.PackAtlases(
                new[] { iconAtlas, uiAtlas }, EditorUserBuildSettings.activeBuildTarget);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Vertigo] SpriteAtlasBuilder: packed Atlas_Icons and Atlas_UIChrome.");
        }

        private static SpriteAtlas BuildAtlas(string path, string[] sourceFolders)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = 4,
            });

            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                filterMode = FilterMode.Bilinear,
                sRGB = true,
            });

            TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platformSettings.maxTextureSize = 2048;
            platformSettings.format = TextureImporterFormat.Automatic;
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            atlas.SetPlatformSettings(platformSettings);

            var folders = new List<Object>();
            foreach (string folder in sourceFolders)
            {
                var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folder);
                if (folderAsset != null)
                    folders.Add(folderAsset);
                else
                    Debug.LogWarning($"[Vertigo] SpriteAtlasBuilder: folder not found: {folder}");
            }

            Object[] existing = atlas.GetPackables();
            if (existing.Length > 0) atlas.Remove(existing);
            atlas.Add(folders.ToArray());

            EditorUtility.SetDirty(atlas);
            return atlas;
        }
    }
}
