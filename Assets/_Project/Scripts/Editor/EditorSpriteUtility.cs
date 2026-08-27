using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Exact-filename sprite lookup under the project's art root, shared by every editor tool that wires
    /// sprites onto generated assets or scene objects.
    /// <para>
    /// <see cref="AssetDatabase.FindAssets"/> matches substrings, and the demo art has a live case of that:
    /// <c>UI_Icons_Pistol_Points</c> is a prefix of <c>UI_Icons_Pistol_Points_</c>. Comparing the filename
    /// exactly is load-bearing, not defensive polish.
    /// </para>
    /// </summary>
    public static class EditorSpriteUtility
    {
        public const string SpriteRoot = "Assets/_Project/Art/Sprites";

        public static Sprite FindSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;

            string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite", new[] { SpriteRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!string.Equals(Path.GetFileNameWithoutExtension(path), spriteName, StringComparison.Ordinal))
                    continue;

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
            }

            return null;
        }
    }
}
