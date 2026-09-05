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
            Assert.That(RustlineNativePixelPresentFeature.IsConfigured, Is.True);

            NativePixelViewport expected = NativePixelViewportMath.Calculate(Screen.width, Screen.height);
            Assert.That(presentation.Viewport.LogicalWidth, Is.EqualTo(expected.LogicalWidth));
            Assert.That(presentation.Viewport.LogicalHeight, Is.EqualTo(expected.LogicalHeight));
            AssertTarget(
                presentation.WorldTarget,
                expected,
                NativePixelPresentation.WorldTargetDepthBits);
            AssertTarget(
                presentation.ResolvedTarget,
                expected,
                NativePixelPresentation.ResolvedTargetDepthBits);

            // Consolidated Experiment 2 architecture: the world camera renders stage A;
            // one lightweight utility camera remains active only as the RenderGraph driver.
            Assert.That(presentation.ProcessingCamera.enabled, Is.True);
            Assert.That(presentation.ProcessingCamera.targetTexture, Is.Null);
            Assert.That(presentation.ProcessingCamera.cullingMask, Is.EqualTo(0),
                "The RenderGraph driver camera must not render scene geometry.");
            Assert.That(presentation.PresentedSource, Is.SameAs(presentation.ResolvedTarget));

            UniversalAdditionalCameraData worldCameraData =
                presentation.WorldCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData processingCameraData =
                presentation.ProcessingCamera.GetUniversalAdditionalCameraData();
            ScriptableRenderer utilityRenderer = UniversalRenderPipeline.asset.GetRenderer(
                NativePixelPresentation.UtilityRendererIndex);
            Assert.That(utilityRenderer, Is.Not.Null,
                "The lightweight utility renderer must remain registered in the active URP asset.");
            Assert.That(
                worldCameraData.scriptableRenderer,
                Is.Not.SameAs(utilityRenderer),
                "The gameplay/world camera must remain on the default 2D Renderer.");
            Assert.That(
                processingCameraData.scriptableRenderer,
                Is.SameAs(utilityRenderer),
                "The RenderGraph driver camera must use the lightweight utility renderer.");

            RenderTexture originalWorldTarget = presentation.WorldTarget;
            RenderTexture originalResolvedTarget = presentation.ResolvedTarget;
            presentation.SetPenumbraEnabled(true);
            presentation.SetPenumbraEnabled(true);

            Assert.That(presentation.PenumbraEnabled, Is.True);
            Assert.That(presentation.WorldTarget, Is.SameAs(originalWorldTarget));
            Assert.That(presentation.ResolvedTarget, Is.SameAs(originalResolvedTarget));
            Assert.That(presentation.PresentedSource, Is.SameAs(originalResolvedTarget));

            presentation.SetPenumbraEnabled(false);
            presentation.SetPenumbraEnabled(false);
            yield return null;

            Assert.That(presentation.PenumbraEnabled, Is.False);
            Assert.That(presentation.WorldTarget, Is.SameAs(originalWorldTarget));
            Assert.That(presentation.ResolvedTarget, Is.SameAs(originalResolvedTarget));
            Assert.That(presentation.ProcessingCamera.enabled, Is.True,
                "The driver camera must remain active so raw-world presentation still reaches the backbuffer.");
            Assert.That(presentation.ProcessingCamera.cullingMask, Is.EqualTo(0));
            Assert.That(presentation.PresentedSource, Is.SameAs(originalWorldTarget));

            presentation.TogglePenumbra();
            Assert.That(presentation.PenumbraEnabled, Is.True);
            Assert.That(presentation.ProcessingCamera.enabled, Is.True);
            Assert.That(presentation.PresentedSource, Is.SameAs(originalResolvedTarget));
        }

        [UnityTest]
        public IEnumerator MovementLab_RendersWorldPixelsThroughRawAndPenumbraRenderGraphPaths()
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
                "The RenderGraph penumbra pass did not receive visible world pixels (stage B).");

            RenderTexture physicalTarget = new RenderTexture(
                Mathf.Max(Screen.width, 1),
                Mathf.Max(Screen.height, 1),
                16,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            physicalTarget.Create();

            // Redirect the RenderGraph driver camera's physical output to a probe RT. The
            // renderer feature then exercises the same final presentation pass as the display.
            presentation.ProcessingCamera.targetTexture = physicalTarget;

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
                    "Penumbra OFF did not present raw world pixels through RenderGraph (stage C).");

                presentation.TogglePenumbra();
                yield return null;
                yield return null;
                AssertPhysicalPlayerRegionHasVisibleContent(
                    physicalTarget,
                    presentation,
                    "Penumbra ON did not present resolved world pixels through RenderGraph (stage C).");
            }
            finally
            {
                presentation.ProcessingCamera.targetTexture = null;
                physicalTarget.Release();
                Object.Destroy(physicalTarget);
            }
        }

        private static void AssertTarget(
            RenderTexture target,
            NativePixelViewport expected,
            int expectedDepthBits)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(target.width, Is.EqualTo(expected.LogicalWidth));
            Assert.That(target.height, Is.EqualTo(expected.LogicalHeight));
            Assert.That(target.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(target.antiAliasing, Is.EqualTo(1));
            Assert.That(target.useMipMap, Is.False);
            Assert.That(target.autoGenerateMips, Is.False);
            Assert.That(target.depth, Is.EqualTo(expectedDepthBits));
            Assert.That(target.format, Is.EqualTo(RenderTextureFormat.ARGB32));
            Assert.That(target.sRGB, Is.True);
            Assert.That(target.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(target.anisoLevel, Is.EqualTo(0));
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
