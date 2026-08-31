using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// One-command Android build: locks in the Player Settings the case's target device needs, builds
    /// straight to an installable APK (never an AAB — the brief asks for an APK, and
    /// <see cref="EditorUserBuildSettings.buildAppBundle"/> is forced off here for exactly that reason), and
    /// throws on anything short of success so a CLI invocation
    /// (<c>-executeMethod Vertigo.Wheel.Editor.BuildPipelineRunner.BuildAndroid -quit</c>) returns a
    /// non-zero exit code instead of silently producing nothing.
    /// <para>
    /// <see cref="ApplyAndroidPlayerSettings"/> sets these in code rather than relying on whatever the
    /// Inspector currently shows, so the build is reproducible regardless of what a previous session left
    /// behind — the same reasoning <see cref="GameConfigGenerator"/> uses for the authored data set.
    /// Code signing is deliberately untouched: a release keystore is a per-developer secret (see the
    /// architecture plan's §12), not something a build script should assume or generate.
    /// </para>
    /// </summary>
    public static class BuildPipelineRunner
    {
        private const string OutputDirectory = "Builds/Android";
        private const string Version = "1.0.0";

        private static readonly string[] ScenePaths = { "Assets/_Project/Scenes/Main.unity" };

        [MenuItem("Tools/Vertigo/Build Android APK")]
        public static void BuildAndroid()
        {
            ApplyAndroidPlayerSettings();

            Directory.CreateDirectory(OutputDirectory);
            string outputPath = $"{OutputDirectory}/WheelOfFortune-v{Version}.apk";

            var options = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception(
                    $"[Vertigo] Android build failed: result={summary.result}, " +
                    $"{summary.totalErrors} error(s). See the log above for details.");
            }

            Debug.Log(
                $"[Vertigo] Android build succeeded: {outputPath} " +
                $"({summary.totalSize / (1024f * 1024f):F1} MB) in {summary.totalTime.TotalSeconds:F0}s.");
        }

        /// <summary>Matches §12 of the architecture plan: IL2CPP, ARMv7+ARM64, min API 22, Low stripping,
        /// APK (not AAB), and the landscape-only orientation locked in since Day 1.</summary>
        private static void ApplyAndroidPlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.cagribilginer.wheeloffortune");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            EditorUserBuildSettings.buildAppBundle = false;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }
    }
}
