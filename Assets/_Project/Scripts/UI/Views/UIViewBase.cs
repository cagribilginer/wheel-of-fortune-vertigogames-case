using UnityEngine;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Base for every view. Refs are found by child name and re-bound automatically whenever the object
    /// is edited, so a view never ships with a broken drag-and-drop reference and never needs an Inspector
    /// OnClick binding.
    /// <para>
    /// The naming convention (<c>ui_&lt;widget&gt;_&lt;region&gt;_&lt;detail&gt;</c>) is not just a style
    /// rule here: it is the lookup key. <see cref="Bind{T}"/> is legitimate specifically because the case
    /// mandates that convention, so the name in the hierarchy IS the contract a view binds against.
    /// </para>
    /// </summary>
    public abstract class UIViewBase : MonoBehaviour
    {
        protected abstract void CacheReferences();

        /// <summary>Covers "Add Component" in the editor, where OnValidate does not fire.</summary>
        protected virtual void Reset() => CacheReferences();

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Application.isPlaying) return;

            CacheReferences();

            // OnValidate runs mid-deserialization; calling SetDirty inline triggers "SendMessage cannot be
            // called during OnValidate" in 2021 LTS, so the actual dirtying is deferred one tick.
            UnityEditor.EditorApplication.delayCall += MarkDirtyDeferred;
        }

        private void MarkDirtyDeferred()
        {
            UnityEditor.EditorApplication.delayCall -= MarkDirtyDeferred;
            if (this == null) return; // destroyed while the callback was queued

            UnityEditor.EditorUtility.SetDirty(this);

            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            else if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        /// <summary>
        /// Re-runs the auto-wiring immediately. <c>Reset</c>/<c>OnValidate</c> normally cover this, but an
        /// editor tool that calls <c>AddComponent</c> and needs the refs bound in the same call should not
        /// depend on exactly when the editor decides to invoke either of those.
        /// </summary>
        public void RebindReferences() => CacheReferences();

        /// <summary>
        /// Binds by GameObject name among this view's children. Early-outs when the field already points at
        /// the correctly named object, so steady-state OnValidate calls are O(1) rather than a child sweep.
        /// </summary>
        protected void Bind<T>(ref T field, string nodeName) where T : Component
        {
            if (field != null && field.gameObject.name == nodeName) return;

            T[] candidates = GetComponentsInChildren<T>(includeInactive: true);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].gameObject.name == nodeName)
                {
                    field = candidates[i];
                    return;
                }
            }

            field = null;
        }
    }
}
