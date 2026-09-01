using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rustline.Presentation
{
    /// <summary>
    /// Routes only the native-pixel processing/presentation cameras through the lightweight
    /// Universal Renderer registered at index 1. The gameplay/world camera deliberately
    /// remains on Rustline's default 2D Renderer.
    /// </summary>
    internal static class RustlineUtilityRendererSelector
    {
        private const int UtilityRendererIndex = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyToActivePresentation()
        {
            NativePixelPresentation presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            if (presentation == null)
            {
                return;
            }

            SelectUtilityRenderer(presentation.ProcessingCamera);
            SelectUtilityRenderer(presentation.PresentationCamera);
        }

        private static void SelectUtilityRenderer(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.SetRenderer(UtilityRendererIndex);
        }
    }
}
