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
        private const float SampleWindowSeconds = 2f;
        private const float CopiedFeedbackSeconds = 1f;

        private static readonly Rect ShadowRect = new Rect(11f, 11f, 520f, 96f);
        private static readonly Rect TextRect = new Rect(10f, 10f, 520f, 96f);
        private static readonly Rect HintShadowRect = new Rect(11f, 81f, 520f, 24f);
        private static readonly Rect HintTextRect = new Rect(10f, 80f, 520f, 24f);

        private float elapsedSeconds;
        private float worstFrameSeconds;
        private float copiedFeedbackUntil;
        private int frameCount;
        private string displayText = "PERF 2.0s\nMeasuring...";
        private GUIStyle foregroundStyle;
        private GUIStyle shadowStyle;
        private GUIStyle hintStyle;
        private GUIStyle hintShadowStyle;

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
                $"PERF {SampleWindowSeconds:0.0}s  {Screen.width}x{Screen.height}  |  FRAMES {frameCount}\n" +
                $"FPS {framesPerSecond:0.0}  |  AVG {averageFrameSeconds * 1000f:0.00} ms  |  WORST {worstFrameSeconds * 1000f:0.00} ms\n" +
                $"VSync {QualitySettings.vSyncCount}  |  Target {Application.targetFrameRate}";

            elapsedSeconds = 0f;
            worstFrameSeconds = 0f;
            frameCount = 0;
        }

        private void OnGUI()
        {
            EnsureStyles();
            HandleCopyClick();

            GUI.Label(ShadowRect, displayText, shadowStyle);
            GUI.Label(TextRect, displayText, foregroundStyle);

            string hint = Time.realtimeSinceStartup < copiedFeedbackUntil
                ? "COPIED TO CLIPBOARD"
                : "CLICK PERF INFO TO COPY";
            GUI.Label(HintShadowRect, hint, hintShadowStyle);
            GUI.Label(HintTextRect, hint, hintStyle);
        }

        private void HandleCopyClick()
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || !TextRect.Contains(current.mousePosition))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = displayText;
            copiedFeedbackUntil = Time.realtimeSinceStartup + CopiedFeedbackSeconds;
            current.Use();
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

            hintStyle = new GUIStyle(foregroundStyle)
            {
                fontSize = 11
            };
            hintStyle.normal.textColor = new Color32(201, 187, 177, 255); // Light Metal

            hintShadowStyle = new GUIStyle(hintStyle);
            hintShadowStyle.normal.textColor = new Color32(1, 2, 11, 255); // Deep Space
        }
    }
}
#endif
