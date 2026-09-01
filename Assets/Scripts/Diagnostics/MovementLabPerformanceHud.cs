#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Diagnostics
{
    /// <summary>
    /// Tiny, development-only frame-time readout for MovementLab.
    /// It exists to give optimization work a repeatable baseline without changing gameplay.
    /// </summary>
    public sealed class MovementLabPerformanceHud : MonoBehaviour
    {
        private const string MovementLabSceneName = "MovementLab";
        private const float SampleWindowSeconds = 0.5f;

        private static readonly Rect ShadowRect = new Rect(11f, 11f, 460f, 96f);
        private static readonly Rect TextRect = new Rect(10f, 10f, 460f, 96f);

        private float elapsedSeconds;
        private float worstFrameSeconds;
        private int frameCount;
        private string displayText = "PERF 0.5s\nMeasuring...";
        private GUIStyle foregroundStyle;
        private GUIStyle shadowStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForMovementLab()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, MovementLabSceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<MovementLabPerformanceHud>() != null)
            {
                return;
            }

            GameObject hudObject = new GameObject("Performance HUD - Diagnostic")
            {
                hideFlags = HideFlags.DontSave
            };
            hudObject.AddComponent<MovementLabPerformanceHud>();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaTime;
            frameCount++;
            worstFrameSeconds = Mathf.Max(worstFrameSeconds, deltaTime);

            if (elapsedSeconds < SampleWindowSeconds)
            {
                return;
            }

            float averageFrameSeconds = elapsedSeconds / frameCount;
            float framesPerSecond = frameCount / elapsedSeconds;
            displayText =
                $"PERF {SampleWindowSeconds:0.0}s  {Screen.width}x{Screen.height}\n" +
                $"FPS {framesPerSecond:0.0}  |  AVG {averageFrameSeconds * 1000f:0.00} ms  |  WORST {worstFrameSeconds * 1000f:0.00} ms\n" +
                $"VSync {QualitySettings.vSyncCount}  |  Target {Application.targetFrameRate}";

            elapsedSeconds = 0f;
            worstFrameSeconds = 0f;
            frameCount = 0;
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Label(ShadowRect, displayText, shadowStyle);
            GUI.Label(TextRect, displayText, foregroundStyle);
        }

        private void EnsureStyles()
        {
            if (foregroundStyle != null)
            {
                return;
            }

            foregroundStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false
            };
            foregroundStyle.normal.textColor = new Color32(32, 237, 229, 255); // Neon Cyan

            shadowStyle = new GUIStyle(foregroundStyle);
            shadowStyle.normal.textColor = new Color32(1, 2, 11, 255); // Deep Space
        }
    }
}
#endif
