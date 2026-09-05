#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rustline.Diagnostics
{
    /// <summary>
    /// Tiny, development-only frame-time readout for MovementLab.
    /// It exists to give optimization work a repeatable baseline without changing gameplay.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class MovementLabPerformanceHud : MonoBehaviour
    {
        private const string MovementLabSceneName = "MovementLab";
        private const float SampleWindowSeconds = 2f;
        private const float FeedbackSeconds = 1f;

        private static readonly Rect ShadowRect = new Rect(11f, 11f, 760f, 112f);
        private static readonly Rect TextRect = new Rect(10f, 10f, 760f, 112f);
        private static readonly Rect HintShadowRect = new Rect(11f, 101f, 760f, 24f);
        private static readonly Rect HintTextRect = new Rect(10f, 100f, 760f, 24f);

        private float elapsedSeconds;
        private float worstFrameSeconds;
        private float feedbackUntil;
        private float lastAverageFrameSeconds;
        private float lastFramesPerSecond;
        private float lastWorstFrameSeconds;
        private int frameCount;
        private int lastFrameCount;
        private bool hudVisible;
        private string displayText = "PERF 2.0s\nMeasuring...";
        private string feedbackText;
        private PixelCameraFollow2D cameraFollow;
        private NativePixelPresentation presentation;
        private GUIStyle foregroundStyle;
        private GUIStyle shadowStyle;
        private GUIStyle hintStyle;
        private GUIStyle hintShadowStyle;

        public bool IsVisible => hudVisible;
        public int SampleFrameCount => frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForMovementLab()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, MovementLabSceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (UnityEngine.Object.FindAnyObjectByType<MovementLabPerformanceHud>() != null)
            {
                return;
            }

            GameObject hudObject = new GameObject("Performance HUD - Diagnostic")
            {
                hideFlags = HideFlags.DontSave
            };
            hudObject.AddComponent<MovementLabPerformanceHud>();
        }

        private void Awake()
        {
            cameraFollow = UnityEngine.Object.FindAnyObjectByType<PixelCameraFollow2D>();
            presentation = UnityEngine.Object.FindAnyObjectByType<NativePixelPresentation>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
            {
                TogglePenumbra();
            }

            if (keyboard != null &&
                (keyboard.hKey.wasPressedThisFrame || keyboard.f3Key.wasPressedThisFrame))
            {
                SetHudVisible(!hudVisible);
            }

            if (!hudVisible)
            {
                return;
            }

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

            lastAverageFrameSeconds = elapsedSeconds / frameCount;
            lastFramesPerSecond = frameCount / elapsedSeconds;
            lastWorstFrameSeconds = worstFrameSeconds;
            lastFrameCount = frameCount;
            RefreshDisplayText();

            ResetSample();
        }

        private void OnGUI()
        {
            if (!hudVisible)
            {
                return;
            }

            EnsureStyles();
            HandleHudClick();

            GUI.Label(ShadowRect, displayText, shadowStyle);
            GUI.Label(TextRect, displayText, foregroundStyle);

            string hint = Time.realtimeSinceStartup < feedbackUntil
                ? feedbackText
                : "LEFT CLICK COPY  |  RIGHT CLICK TOGGLE CAMERA FOLLOW  |  P PENUMBRA  |  H/F3 HIDE";
            GUI.Label(HintShadowRect, hint, hintShadowStyle);
            GUI.Label(HintTextRect, hint, hintStyle);
        }

        private void HandleHudClick()
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || !TextRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.button == 0)
            {
                RefreshDisplayText();
                GUIUtility.systemCopyBuffer = displayText;
                ShowFeedback("COPIED TO CLIPBOARD");
                current.Use();
                return;
            }

            if (current.button != 1)
            {
                return;
            }

            if (cameraFollow == null)
            {
                cameraFollow = UnityEngine.Object.FindAnyObjectByType<PixelCameraFollow2D>();
            }

            if (cameraFollow == null)
            {
                ShowFeedback("CAMERA FOLLOW NOT FOUND");
                current.Use();
                return;
            }

            cameraFollow.enabled = !cameraFollow.enabled;
            ResetSample();
            ShowFeedback(cameraFollow.enabled ? "CAMERA FOLLOW ON" : "CAMERA FOLLOW FROZEN");
            current.Use();
        }

        private string GetCameraFollowState()
        {
            if (cameraFollow == null)
            {
                return "MISSING";
            }

            return cameraFollow.enabled ? "ON" : "FROZEN";
        }

        private void TogglePenumbra()
        {
            if (presentation == null)
            {
                presentation = UnityEngine.Object.FindAnyObjectByType<NativePixelPresentation>();
            }

            if (presentation == null)
            {
                ShowFeedback("PENUMBRA PRESENTATION NOT FOUND");
                return;
            }

            presentation.TogglePenumbra();
            ResetSample();
            if (hudVisible)
            {
                RefreshDisplayText();
                ShowFeedback(presentation.PenumbraEnabled ? "PENUMBRA ON" : "PENUMBRA OFF");
            }
        }

        private void SetHudVisible(bool visible)
        {
            hudVisible = visible;
            ResetSample();
            if (!hudVisible)
            {
                return;
            }

            RefreshDisplayText();
            ShowFeedback("PERFORMANCE HUD ON");
        }

        private void RefreshDisplayText()
        {
            NativePixelViewport viewport = presentation != null
                ? presentation.Viewport
                : NativePixelViewportMath.Calculate(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1));
            string penumbraState = presentation == null
                ? "MISSING"
                : (presentation.PenumbraEnabled ? "ON" : "OFF");

            displayText =
                $"PERF {SampleWindowSeconds:0.0}s  PHYSICAL {Screen.width}x{Screen.height}  |  " +
                $"LOGICAL {viewport.LogicalWidth}x{viewport.LogicalHeight}  |  SCALE {viewport.IntegerScale}x\n" +
                $"FPS {lastFramesPerSecond:0.0}  |  AVG {lastAverageFrameSeconds * 1000f:0.00} ms  |  " +
                $"WORST {lastWorstFrameSeconds * 1000f:0.00} ms  |  FRAMES {lastFrameCount}\n" +
                $"VSync {QualitySettings.vSyncCount}  |  Target {Application.targetFrameRate}  |  " +
                $"Camera {GetCameraFollowState()}  |  Penumbra {penumbraState}";
        }

        private void ResetSample()
        {
            elapsedSeconds = 0f;
            worstFrameSeconds = 0f;
            frameCount = 0;
        }

        private void ShowFeedback(string text)
        {
            feedbackText = text;
            feedbackUntil = Time.realtimeSinceStartup + FeedbackSeconds;
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
