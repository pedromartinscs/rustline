using System.Collections;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class NativePixelPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator MovementLab_AllocatesAndTogglesNativeLogicalPresentation()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            NativePixelPresentation presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.PenumbraEnabled, Is.True);
            Assert.That(presentation.HasAllocatedTargets, Is.True);

            NativePixelViewport expected = NativePixelViewportMath.Calculate(Screen.width, Screen.height);
            Assert.That(presentation.Viewport.LogicalWidth, Is.EqualTo(expected.LogicalWidth));
            Assert.That(presentation.Viewport.LogicalHeight, Is.EqualTo(expected.LogicalHeight));
            AssertTarget(presentation.WorldTarget, expected, requiresCameraDepth: true);
            AssertTarget(presentation.ResolvedTarget, expected, requiresCameraDepth: true);
            Assert.That(presentation.ProcessingCamera.enabled, Is.True);
            Assert.That(presentation.ProcessingRenderer.enabled, Is.True);
            Assert.That(presentation.PresentationCamera.enabled, Is.True);
            Assert.That(presentation.PresentationRenderer.enabled, Is.True);
            Assert.That(presentation.PresentedSource, Is.SameAs(presentation.ResolvedTarget));

            UniversalAdditionalCameraData worldCameraData =
                presentation.WorldCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData processingCameraData =
                presentation.ProcessingCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData presentationCameraData =
                presentation.PresentationCamera.GetUniversalAdditionalCameraData();
            Assert.That(
                worldCameraData.rendererIndex,
                Is.Not.EqualTo(NativePixelPresentation.UtilityRendererIndex),
                "The gameplay/world camera must remain on the default 2D Renderer.");
            Assert.That(
                processingCameraData.rendererIndex,
                Is.EqualTo(NativePixelPresentation.UtilityRendererIndex),
                "The logical penumbra camera must use the lightweight utility renderer.");
            Assert.That(
                presentationCameraData.rendererIndex,
                Is.EqualTo(NativePixelPresentation.UtilityRendererIndex),
                "The physical presentation camera must use the lightweight utility renderer.");

            RenderTexture originalWorldTarget = presentation.WorldTarget;
            RenderTexture originalResolvedTarget = presentation.ResolvedTarget;
            presentation.TogglePenumbra();
            yield return null;

            Assert.That(presentation.PenumbraEnabled, Is.False);
            Assert.That(presentation.WorldTarget, Is.SameAs(originalWorldTarget));
            Assert.That(presentation.ResolvedTarget, Is.SameAs(originalResolvedTarget));
            Assert.That(presentation.ProcessingCamera.enabled, Is.False);
            Assert.That(presentation.ProcessingRenderer.enabled, Is.False);
            Assert.That(presentation.PresentedSource, Is.SameAs(originalWorldTarget));
            presentation.TogglePenumbra();
            Assert.That(presentation.PenumbraEnabled, Is.True);
            Assert.That(presentation.ProcessingCamera.enabled, Is.True);
            Assert.That(presentation.ProcessingRenderer.enabled, Is.True);
            Assert.That(presentation.PresentedSource, Is.SameAs(originalResolvedTarget));
        }

        [UnityTest]
        public IEnumerator MovementLab_RendersWorldPixelsThroughRawAndPenumbraPaths()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;
            yield return null;

            NativePixelPresentation presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            Assert.That(presentation, Is.Not.Null);

            AssertPlayerRegionHasVisibleContent(
                presentation.WorldTarget,
                presentation,
                "The World Camera target remained uniformly Deep Space near the player (stage A).");
            AssertPlayerRegionHasVisibleContent(
                presentation.ResolvedTarget,
                presentation,
                "The penumbra target did not receive visible world pixels (stage B).");

            RenderTexture physicalTarget = new RenderTexture(
                Mathf.Max(Screen.width, 1),
                Mathf.Max(Screen.height, 1),
                16,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            physicalTarget.Create();
            presentation.PresentationCamera.targetTexture = physicalTarget;

            if (presentation.PenumbraEnabled)
            {
                presentation.TogglePenumbra();
            }

            try
            {
                yield return null;
                yield return null;
                AssertPhysicalPlayerRegionHasVisibleContent(
                    physicalTarget,
                    presentation,
                    "Penumbra OFF did not present raw world pixels to the physical framebuffer (stage C).");

                presentation.TogglePenumbra();
                yield return null;
                yield return null;
                AssertPhysicalPlayerRegionHasVisibleContent(
                    physicalTarget,
                    presentation,
                    "Penumbra ON did not present resolved world pixels to the physical framebuffer (stage C).");
            }
            finally
            {
                presentation.PresentationCamera.targetTexture = null;
                physicalTarget.Release();
                Object.Destroy(physicalTarget);
            }
        }

        private static void AssertTarget(
            RenderTexture target,
            NativePixelViewport expected,
            bool requiresCameraDepth)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(target.width, Is.EqualTo(expected.LogicalWidth));
            Assert.That(target.height, Is.EqualTo(expected.LogicalHeight));
            Assert.That(target.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(target.antiAliasing, Is.EqualTo(1));
            Assert.That(target.useMipMap, Is.False);
            Assert.That(target.autoGenerateMips, Is.False);
            Assert.That(target.depth > 0, Is.EqualTo(requiresCameraDepth));
        }

        private static void AssertPlayerRegionHasVisibleContent(
            RenderTexture target,
            NativePixelPresentation presentation,
            string message)
        {
            Vector3 viewportPoint = presentation.WorldCamera.WorldToViewportPoint(
                presentation.PlayerTarget.position + Vector3.up);
            int centerX = Mathf.RoundToInt(viewportPoint.x * target.width);
            int centerY = Mathf.RoundToInt(viewportPoint.y * target.height);
            AssertRenderTextureRegionHasVisibleContent(target, centerX, centerY, 96, message);
        }

        private static void AssertPhysicalPlayerRegionHasVisibleContent(
            RenderTexture physicalTarget,
            NativePixelPresentation presentation,
            string message)
        {
            Vector3 viewportPoint = presentation.WorldCamera.WorldToViewportPoint(
                presentation.PlayerTarget.position + Vector3.up);
            NativePixelViewport viewport = presentation.Viewport;
            int centerX = Mathf.RoundToInt(
                viewport.OutputOffsetX + viewportPoint.x * viewport.LogicalWidth * viewport.IntegerScale);
            int centerY = Mathf.RoundToInt(
                viewport.OutputOffsetY + viewportPoint.y * viewport.LogicalHeight * viewport.IntegerScale);
            int size = Mathf.Max(96 * viewport.IntegerScale, 96);
            AssertRenderTextureRegionHasVisibleContent(physicalTarget, centerX, centerY, size, message);
        }

        private static void AssertRenderTextureRegionHasVisibleContent(
            RenderTexture target,
            int centerX,
            int centerY,
            int size,
            string message)
        {
            int width = Mathf.Min(size, target.width);
            int height = Mathf.Min(size, target.height);
            int x = Mathf.Clamp(centerX - width / 2, 0, target.width - width);
            int y = Mathf.Clamp(centerY - height / 2, 0, target.height - height);
            RenderTexture previous = RenderTexture.active;
            Texture2D probe = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                RenderTexture.active = target;
                probe.ReadPixels(new Rect(x, y, width, height), 0, 0, false);
                probe.Apply(false, false);
                AssertPixelsContainVisibleContent(probe.GetPixels32(), message);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(probe);
            }
        }

        private static void AssertPixelsContainVisibleContent(Color32[] pixels, string message)
        {
            Color32 deepSpace = new Color32(1, 2, 11, 255);
            int visiblyDifferentPixels = 0;
            foreach (Color32 pixel in pixels)
            {
                int difference = Mathf.Abs(pixel.r - deepSpace.r) +
                                 Mathf.Abs(pixel.g - deepSpace.g) +
                                 Mathf.Abs(pixel.b - deepSpace.b);
                if (difference > 24)
                {
                    visiblyDifferentPixels++;
                }
            }

            Assert.That(visiblyDifferentPixels, Is.GreaterThan(8), message);
        }
    }
}
