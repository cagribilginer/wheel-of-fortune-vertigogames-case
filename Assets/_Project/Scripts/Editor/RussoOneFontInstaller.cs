using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// One-shot: builds the Russo One TextMesh Pro font asset and assigns it to every TMP text in the UI prefabs and the
    /// scene (nested prefab instances are left to their own prefab). Static texts that no longer fit are listed.
    /// </summary>
    internal static class RussoOneFontInstaller
    {
        private const string FONT_PATH = "Assets/_Project/Art/Fonts/RussoOne-Regular.ttf";
        private const string ASSET_PATH = "Assets/_Project/Art/Fonts/RussoOne SDF.asset";
        private const string PREFAB_FOLDER = "Assets/_Project/Prefabs/UI";
        private const string SCENE_PATH = "Assets/_Project/Scenes/Main.unity";
        private const int SAMPLING_POINT_SIZE = 90;
        private const int ATLAS_PADDING = 9;
        private const int ATLAS_SIZE = 1024;

        // Printable ASCII plus the Turkish letters and the few symbols the UI shows.
        private const string EXTRA_CHARACTERS = "\u00c7\u011e\u0130\u00d6\u015e\u00dc\u00e7\u011f\u0131\u00f6\u015f\u00fc\u00d7\u2022\u2026";

        [MenuItem("Tools/Vertigo/Apply Russo One Font")]
        private static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ASSET_PATH);
            if (!asset) asset = CreateFontAsset();
            if (!asset) return;

            var tight = new List<string>();
            int changed = ReplaceInPrefabs(asset, tight) + ReplaceInScene(asset, tight);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Vertigo] Russo One applied to {changed} text(s).");
            foreach (string line in tight) Debug.LogWarning($"[Vertigo] Wider than its box with Russo One: {line}");
        }

        private static TMP_FontAsset CreateFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FONT_PATH);
            if (!font)
            {
                Debug.LogError($"[Vertigo] Font not found at {FONT_PATH}.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                font, SAMPLING_POINT_SIZE, ATLAS_PADDING, GlyphRenderMode.SDFAA, ATLAS_SIZE, ATLAS_SIZE,
                AtlasPopulationMode.Dynamic, true);
            asset.name = "RussoOne SDF";
            AssetDatabase.CreateAsset(asset, ASSET_PATH);

            asset.material.name = "RussoOne SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = "RussoOne SDF Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }

            var characters = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) characters.Append(c);
            characters.Append(EXTRA_CHARACTERS);
            asset.TryAddCharacters(characters.ToString(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[Vertigo] Russo One has no glyph for: {missing}");

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static int ReplaceInPrefabs(TMP_FontAsset asset, List<string> tight)
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PREFAB_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int inPrefab = Replace(root.GetComponentsInChildren<TMP_Text>(true), asset, tight, path);
                if (inPrefab > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                changed += inPrefab;
            }
            return changed;
        }

        private static int ReplaceInScene(TMP_FontAsset asset, List<string> tight)
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                changed += Replace(root.GetComponentsInChildren<TMP_Text>(true), asset, tight, SCENE_PATH);

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return changed;
        }

        private static int Replace(TMP_Text[] texts, TMP_FontAsset asset, List<string> tight, string owner)
        {
            int changed = 0;
            foreach (TMP_Text text in texts)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(text)) continue;
                if (text.font == asset) continue;

                text.font = asset;
                text.fontSharedMaterial = asset.material;
                text.ForceMeshUpdate();
                changed++;

                bool isTight = !text.enableWordWrapping && text.preferredWidth > text.rectTransform.rect.width + 1f;
                if (isTight) tight.Add($"{owner} / {text.name} ('{text.text}')");
            }
            return changed;
        }
    }
}
