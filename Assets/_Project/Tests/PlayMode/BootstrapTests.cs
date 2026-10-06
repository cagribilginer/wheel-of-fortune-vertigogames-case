using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using Vertigo.Wheel.Core.States.Flow;
using Vertigo.Wheel.Gameplay;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Tests.PlayMode
{
    /// <summary>
    /// The one Play Mode test in the suite (architecture plan §8): proves the composition root actually
    /// wires up in a real scene — <c>GameInstallerMono</c> loads its Addressable configs, builds the state
    /// machine, and the flow reaches <c>IdleState</c> — rather than just that the pure logic behind it is
    /// correct in isolation. Everything else is proven cheaper and faster in Edit Mode; this file stays a
    /// suite of one on purpose.
    /// </summary>
    public sealed class BootstrapTests
    {
        private const string SCENE_PATH = "Assets/_Project/Scenes/Main.unity";
        private const float TIMEOUT_SECONDS = 2f;

        [UnityTest]
        public IEnumerator Scene_Loads_AndReachesIdle_WithinTwoSeconds()
        {
            // Load by path rather than by build index. This editor API is the correct way to do that from
            // inside a running Play Mode session; the on-device fallback below is untested since this
            // project only ever runs Play Mode tests from the editor (§3 row 18 — CI, and by extension
            // device test runs, is out of scope).
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(SCENE_PATH, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(SCENE_PATH);
#endif

            // One frame for the freshly loaded scene's Awake() calls — GameInstallerMono's composition root in
            // particular — to actually run before anything below is safe to look up.
            yield return null;

            var installer = Object.FindObjectOfType<GameInstallerMono>();
            Assert.IsNotNull(installer, $"No GameInstallerMono found after loading '{SCENE_PATH}'.");
            Assert.IsNotNull(installer.Machine, "GameInstallerMono.Awake() did not construct a GameStateMachine.");

            // BootState -> ZoneSetupState -> IdleState is not synchronous: ZoneSetupState's ShowZone call
            // only reaches IdleState once the zone map's scroll tween completes, so this has to poll rather
            // than assert immediately. Any exception thrown during that chain surfaces as a LogType.Exception
            // entry, which fails a [UnityTest] on its own — the "no unhandled exceptions" requirement needs
            // no separate assertion here.
            float deadline = Time.realtimeSinceStartup + TIMEOUT_SECONDS;
            while (!installer.Machine.IsIn<IdleState>() && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsInstanceOf<IdleState>(
                installer.Machine.Current,
                $"Expected IdleState within {TIMEOUT_SECONDS}s of loading '{SCENE_PATH}'; the state machine is " +
                $"still in {installer.Machine.Current?.GetType().Name ?? "<none>"}.");

            Assert.IsNotNull(Object.FindObjectOfType<WheelViewMono>(), $"No WheelViewMono found in '{SCENE_PATH}'.");
        }
    }
}
