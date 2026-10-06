using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// One-shot: turns the scene's big UI panels into prefab instances in place, like dragging each into the Project
    /// window. Nothing is changed unless every panel passes the check that none of its components points at a scene
    /// object outside it, since a prefab asset cannot keep such a reference.
    /// </summary>
    internal static class ScenePanelPrefabConverter
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/Main.unity";
        private const string PREFAB_FOLDER = "Assets/_Project/Prefabs/UI";

        private static readonly string[] s_panelNames =
        {
            "ui_panel_wheel",
            "ui_panel_zonemap",
            "ui_panel_bank",
            "ui_panel_actions",
            "ui_popup_bomb",
            "ui_popup_collect",
            "ui_popup_milestone",
            "ui_panel_vfx_layer",
        };

        [MenuItem("Tools/Vertigo/Convert Scene Panels To Prefabs")]
        private static void Convert()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            var panels = new List<GameObject>();
            foreach (string panelName in s_panelNames)
            {
                GameObject panel = FindUnique(scene, panelName);
                if (!panel) return;
                if (PrefabUtility.IsAnyPrefabInstanceRoot(panel)) continue;

                panels.Add(panel);
            }

            foreach (GameObject panel in panels)
            {
                if (!HasNoOutsideReferences(panel)) return;
            }

            EnsureFolder();
            foreach (GameObject panel in panels)
            {
                string path = $"{PREFAB_FOLDER}/{panel.name}.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(panel, path, InteractionMode.AutomatedAction);
                Debug.Log($"[Vertigo] Prefab created: {path}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Vertigo] Converted {panels.Count} panel(s) to prefabs and saved {SCENE_PATH}.");
        }

        private static GameObject FindUnique(Scene scene, string panelName)
        {
            var matches = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == panelName) matches.Add(child.gameObject);
                }
            }

            if (matches.Count == 1) return matches[0];

            Debug.LogError($"[Vertigo] Expected exactly one '{panelName}' in the scene, found {matches.Count}. Nothing was changed.");
            return null;
        }

        private static bool HasNoOutsideReferences(GameObject panel)
        {
            bool isClean = true;
            foreach (Component component in panel.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;

                SerializedProperty property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (!IsOutsideReference(property.objectReferenceValue, panel)) continue;

                    isClean = false;
                    Debug.LogError(
                        $"[Vertigo] {component.name}/{component.GetType().Name}.{property.propertyPath} points at " +
                        $"'{property.objectReferenceValue.name}', outside '{panel.name}'. Nothing was changed.", component);
                }
            }
            return isClean;
        }

        private static bool IsOutsideReference(Object target, GameObject panel)
        {
            if (!target) return false;

            var component = target as Component;
            GameObject targetObject = component ? component.gameObject : target as GameObject;
            if (!targetObject || !targetObject.scene.IsValid()) return false;

            return !targetObject.transform.IsChildOf(panel.transform);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
        }
    }
}
