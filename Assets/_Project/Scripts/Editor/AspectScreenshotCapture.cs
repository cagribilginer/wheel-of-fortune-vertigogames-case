using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Saves the running game at the three aspect ratios the brief asks for (20:9, 16:9, 4:3) into <c>Screenshots/</c>.
    /// Each size is rendered into its own RenderTexture with the root canvas switched to Screen Space - Camera for the
    /// moment, so the result does not depend on the size of the Game window; everything is put back afterwards.
    /// </summary>
    internal static class AspectScreenshotCapture
    {
        private const string OUTPUT_DIRECTORY = "Screenshots";
        private const int SETTLE_FRAMES = 3;
        private const int DEPTH_BITS = 24;
        private const int ANTI_ALIASING = 4;
        private const float CANVAS_PLANE_DISTANCE = 100f;

        private static readonly (string Name, int Width, int Height)[] s_sizes =
        {
            ("20-9", 2400, 1080),
            ("16-9", 1920, 1080),
            ("4-3", 2048, 1536),
        };

        private static Camera s_camera;
        private static RenderTexture s_originalTarget;
        private static readonly List<CanvasState> s_canvases = new();
        private static RenderTexture s_texture;
        private static int s_index;
        private static int s_captureAtFrame;
        private static bool s_isRunning;

        private struct CanvasState
        {
            public Canvas Canvas;
            public RenderMode Mode;
            public Camera WorldCamera;
            public float PlaneDistance;
        }

        [MenuItem("Tools/Vertigo/Capture Aspect Screenshots")]
        private static void Capture()
        {
            if (s_isRunning) return;

            s_camera = Camera.main ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
            if (!s_camera)
            {
                Debug.LogError("[Vertigo] Screenshots: no camera in the scene.");
                return;
            }

            s_originalTarget = s_camera.targetTexture;
            s_canvases.Clear();
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (!canvas.isRootCanvas) continue;

                s_canvases.Add(new CanvasState
                {
                    Canvas = canvas,
                    Mode = canvas.renderMode,
                    WorldCamera = canvas.worldCamera,
                    PlaneDistance = canvas.planeDistance,
                });
            }

            Directory.CreateDirectory(OUTPUT_DIRECTORY);
            s_index = 0;
            s_isRunning = true;
            BeginSize();
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Vertigo/Capture Aspect Screenshots", true)]
        private static bool CanCapture()
        {
            return Application.isPlaying && !s_isRunning;
        }

        private static void BeginSize()
        {
            var (_, width, height) = s_sizes[s_index];

            s_texture = new RenderTexture(width, height, DEPTH_BITS) { antiAliasing = ANTI_ALIASING };
            s_camera.targetTexture = s_texture;
            foreach (CanvasState state in s_canvases)
            {
                state.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                state.Canvas.worldCamera = s_camera;
                state.Canvas.planeDistance = CANVAS_PLANE_DISTANCE;
            }

            // The canvas scaler and the layout groups settle over the next few frames.
            s_captureAtFrame = Time.frameCount + SETTLE_FRAMES;
        }

        private static void Tick()
        {
            try
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("[Vertigo] Screenshots: play mode ended before all sizes were saved.");
                    Finish();
                    return;
                }

                if (Time.frameCount < s_captureAtFrame) return;

                SaveCurrentSize();
                s_index++;
                if (s_index < s_sizes.Length) BeginSize();
                else Finish();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish();
            }
        }

        private static void SaveCurrentSize()
        {
            var (name, width, height) = s_sizes[s_index];

            s_camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = s_texture;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string path = Path.Combine(OUTPUT_DIRECTORY, $"gameplay-aspect-{name}.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            Debug.Log($"[Vertigo] Screenshot saved: {path} ({width}x{height})");
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            s_isRunning = false;

            if (s_camera) s_camera.targetTexture = s_originalTarget;
            foreach (CanvasState state in s_canvases)
            {
                if (!state.Canvas) continue;

                state.Canvas.renderMode = state.Mode;
                state.Canvas.worldCamera = state.WorldCamera;
                state.Canvas.planeDistance = state.PlaneDistance;
            }

            if (s_texture)
            {
                s_texture.Release();
                UnityEngine.Object.DestroyImmediate(s_texture);
            }
            s_texture = null;
            s_canvases.Clear();
        }
    }
}
