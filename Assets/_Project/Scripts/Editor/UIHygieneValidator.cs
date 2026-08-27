using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Walks the loaded scene(s) plus every prefab under <c>Assets/_Project/Prefabs</c>, checking the first
    /// five UI hygiene rules from the architecture plan.
    /// <para>
    /// Day 3 ships only rules 1-5 (raycast target, TMP raycast target, the Maskable trap in both
    /// directions, and Sliced-vs-bordered-sprite). Inspector-binding and legacy-Text checks (rules 7-8) need
    /// nothing to run against yet since no OnClick bindings or legacy Text exist; the naming and scale
    /// checks (9-10) are a later pass once presenters start touching transforms.
    /// </para>
    /// <para>
    /// The rule that makes this worth having: <see cref="MaskableGraphic.maskable"/> == false also disables
    /// <see cref="RectMask2D"/> clipping, not only stencil <see cref="Mask"/>. A single flipped checkbox on
    /// a pooled item either breaks clipping or fails to clip at all, and neither failure is obvious from a
    /// glance at the Game view until the list actually scrolls.
    /// </para>
    /// </summary>
    public sealed class UIHygieneValidator : EditorWindow
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs";

        private readonly List<Finding> _findings = new List<Finding>();
        private Vector2 _scroll;

        private readonly struct Finding
        {
            public readonly int Rule;
            public readonly string Message;
            public readonly UnityEngine.Object Target;

            public Finding(int rule, string message, UnityEngine.Object target)
            {
                Rule = rule;
                Message = message;
                Target = target;
            }
        }

        [MenuItem("Tools/Vertigo/Validate UI Hygiene")]
        private static void Open() => GetWindow<UIHygieneValidator>("UI Hygiene");

        private void OnEnable() => Scan();

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan")) Scan();
            using (new EditorGUI.DisabledScope(_findings.Count == 0))
            {
                if (GUILayout.Button("Fix All")) FixAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(_findings.Count == 0
                ? "No issues found across rules 1-5."
                : $"{_findings.Count} issue(s) found.");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (Finding finding in _findings)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"[Rule {finding.Rule}] {finding.Message}", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = finding.Target;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ scan (read-only, for display)

        private void Scan()
        {
            _findings.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    CollectFindings(root.transform, checkMaskAncestor: true);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null) CollectFindings(prefab.transform, checkMaskAncestor: false);
            }

            Repaint();
        }

        /// <summary>
        /// <paramref name="checkMaskAncestor"/> is false for a prefab asset scanned in isolation: its
        /// eventual runtime parent (a masked scroll view, for the three pooled item prefabs) is not part of
        /// the asset, so rules 3/4 cannot be evaluated meaningfully there and would only produce false
        /// positives on children that are deliberately Maskable in anticipation of being pooled into a mask.
        /// A scene hierarchy's ancestry is real and final, so both rules run there.
        /// </summary>
        private void CollectFindings(Transform root, bool checkMaskAncestor)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                CheckRaycastTarget(graphic);
                if (checkMaskAncestor && graphic is MaskableGraphic maskable) CheckMaskable(maskable);

                if (graphic is Image image) CheckSlicedBorder(image);
            }
        }

        private void CheckRaycastTarget(Graphic graphic)
        {
            if (!graphic.raycastTarget) return;
            if (HasSelfSelectable(graphic) || HasInteractiveAncestor(graphic.transform) || IsBackdrop(graphic)) return;

            int rule = graphic is TMP_Text ? 2 : 1;
            string kind = graphic is TMP_Text ? "TMP text" : "Image";
            _findings.Add(new Finding(rule,
                $"{kind} '{Path(graphic.transform)}' has RaycastTarget ON but is not part of any interactive control.",
                graphic));
        }

        private void CheckMaskable(MaskableGraphic graphic)
        {
            bool hasMaskAncestor = HasMaskAncestor(graphic.transform);

            if (graphic.maskable && !hasMaskAncestor)
            {
                _findings.Add(new Finding(3,
                    $"'{Path(graphic.transform)}' is Maskable but has no Mask/RectMask2D ancestor.",
                    graphic));
            }
            else if (!graphic.maskable && hasMaskAncestor)
            {
                _findings.Add(new Finding(4,
                    $"'{Path(graphic.transform)}' is inside a Mask/RectMask2D but is not Maskable — it will render outside the clip.",
                    graphic));
            }
        }

        private void CheckSlicedBorder(Image image)
        {
            if (image.sprite == null || image.sprite.border == Vector4.zero) return;
            if (image.type == Image.Type.Sliced) return;

            _findings.Add(new Finding(5,
                $"'{Path(image.transform)}' uses sprite '{image.sprite.name}', which has a 9-slice border, " +
                "but the Image type is not Sliced.",
                image));
        }

        // ------------------------------------------------------------------ fix (mutating pass)

        private void FixAll()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    FixHierarchy(root.transform, checkMaskAncestor: true);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    if (FixHierarchy(contents.transform, checkMaskAncestor: false) > 0)
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
            Scan();
        }

        private int FixHierarchy(Transform root, bool checkMaskAncestor)
        {
            int fixedCount = 0;

            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.raycastTarget && !HasSelfSelectable(graphic) && !HasInteractiveAncestor(graphic.transform)
                    && !IsBackdrop(graphic))
                {
                    Undo.RecordObject(graphic, "UI Hygiene: RaycastTarget");
                    graphic.raycastTarget = false;
                    fixedCount++;
                }

                if (checkMaskAncestor && graphic is MaskableGraphic maskable)
                {
                    bool hasMaskAncestor = HasMaskAncestor(graphic.transform);
                    if (maskable.maskable != hasMaskAncestor)
                    {
                        Undo.RecordObject(maskable, "UI Hygiene: Maskable");
                        maskable.maskable = hasMaskAncestor;
                        fixedCount++;
                    }
                }

                if (graphic is Image image && image.sprite != null && image.sprite.border != Vector4.zero
                    && image.type != Image.Type.Sliced)
                {
                    Undo.RecordObject(image, "UI Hygiene: Sliced");
                    image.type = Image.Type.Sliced;
                    fixedCount++;
                }

                EditorUtility.SetDirty(graphic);
            }

            return fixedCount;
        }

        // ------------------------------------------------------------------ helpers

        private static bool HasSelfSelectable(Graphic graphic) => graphic.GetComponent<Selectable>() != null;

        /// <summary>
        /// The third legitimate reason a Graphic keeps RaycastTarget ON: a full-screen popup backdrop that
        /// exists specifically to swallow clicks behind the popup. Recognised by the naming convention
        /// itself, the same way <see cref="UIViewBase.Bind{T}"/> treats the name as the contract.
        /// </summary>
        private static bool IsBackdrop(Graphic graphic) =>
            graphic.gameObject.name.EndsWith("_backdrop", StringComparison.Ordinal);

        private static bool HasInteractiveAncestor(Transform transform)
        {
            Transform parent = transform.parent;
            if (parent == null) return false;

            return parent.GetComponentInParent<Button>() != null
                || parent.GetComponentInParent<ScrollRect>() != null;
        }

        private static bool HasMaskAncestor(Transform transform)
        {
            Transform parent = transform.parent;
            if (parent == null) return false;

            return parent.GetComponentInParent<RectMask2D>() != null
                || parent.GetComponentInParent<Mask>() != null;
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = $"{p.name}/{path}";
            return path;
        }
    }
}
