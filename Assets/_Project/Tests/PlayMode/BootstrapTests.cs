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
    /// wires up in a real scene — <c>GameInstaller</c> loads its Resources-backed configs, builds the state
    /// machine, and the flow reaches <c>IdleState</c> — rather than just that the pure logic behind it is
    /// correct in isolation. Everything else is proven cheaper and faster in Edit Mode; this file stays a
    /// suite of one on purpose.
    /// </summary>
    public sealed class BootstrapTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const float TimeoutSeconds = 2f;

        [UnityTest]
        public IEnumerator Scene_Loads_AndReachesIdle_WithinTwoSeconds()
        {
            // Main.unity is deliberately not in Build Settings (BuildPipelineRunner passes it to
            // BuildPlayer directly), so it has to be loaded by path rather than by build index. This editor
            // API is the correct way to do that from inside a running Play Mode session; the on-device
            // fallback below is untested since this project only ever runs Play Mode tests from the editor
            // (§3 row 18 — CI, and by extension device test runs, is out of scope).
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(ScenePath);
#endif

            // One frame for the freshly loaded scene's Awake() calls — GameInstaller's composition root in
            // particular — to actually run before anything below is safe to look up.
            yield return null;

            var installer = Object.FindObjectOfType<GameInstaller>();
            Assert.IsNotNull(installer, $"No GameInstaller found after loading '{ScenePath}'.");
            Assert.IsNotNull(installer.Machine, "GameInstaller.Awake() did not construct a GameStateMachine.");

            // BootState -> ZoneSetupState -> IdleState is not synchronous: ZoneSetupState's ShowZone call
            // only reaches IdleState once the zone map's scroll tween completes, so this has to poll rather
            // than assert immediately. Any exception thrown during that chain surfaces as a LogType.Exception
            // entry, which fails a [UnityTest] on its own — the "no unhandled exceptions" requirement needs
            // no separate assertion here.
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!installer.Machine.IsIn<IdleState>() && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsInstanceOf<IdleState>(
                installer.Machine.Current,
                $"Expected IdleState within {TimeoutSeconds}s of loading '{ScenePath}'; the state machine is " +
                $"still in {installer.Machine.Current?.GetType().Name ?? "<none>"}.");

            Assert.IsNotNull(Object.FindObjectOfType<WheelView>(), $"No WheelView found in '{ScenePath}'.");
        }
    }
}
