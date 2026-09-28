using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// One-command Android build: applies the Player Settings the target needs, builds an installable APK (never
    /// an AAB) and throws on anything short of success, so a CLI run exits non-zero. Signing is left untouched:
    /// a release keystore is a per-developer secret.
    /// </summary>
    public static class BuildPipelineRunner
    {
        private const string OUTPUT_DIRECTORY = "Builds/Android";

        private static readonly string[] SCENE_PATHS = { "Assets/_Project/Scenes/Main.unity" };

        [MenuItem("Tools/Vertigo/Build Android APK")]
        public static void BuildAndroid()
        {
            ApplyAndroidPlayerSettings();

            Directory.CreateDirectory(OUTPUT_DIRECTORY);
            // Named from the Player Settings version, the one the APK itself reports, so the two can't drift.
            string outputPath = $"{OUTPUT_DIRECTORY}/WheelOfFortune-v{PlayerSettings.bundleVersion}.apk";

            var options = new BuildPlayerOptions
            {
                scenes = SCENE_PATHS,
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
        /// APK (not AAB), and the landscape-only orientation.</summary>
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
