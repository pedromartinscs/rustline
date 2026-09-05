using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace Rustline.Tests
{
    public sealed class NativePixelViewportMathTests
    {
        [TestCase(128, 128, 1, 128, 128, 128, 128, 0, 0)]
        [TestCase(800, 600, 1, 800, 600, 800, 600, 0, 0)]
        [TestCase(1086, 420, 1, 1072, 420, 1072, 420, 7, 0)]
        [TestCase(1920, 1080, 1, 1072, 1072, 1072, 1072, 424, 4)]
        [TestCase(2560, 1440, 1, 1072, 1072, 1072, 1072, 744, 184)]
        [TestCase(3840, 2160, 2, 1072, 1072, 2144, 2144, 848, 8)]
        [TestCase(5760, 3240, 3, 1072, 1072, 3216, 3216, 1272, 12)]
        public void ReferenceCase_MatchesSpecification(
            int physicalWidth,
            int physicalHeight,
            int expectedScale,
            int expectedLogicalWidth,
            int expectedLogicalHeight,
            int expectedOutputWidth,
            int expectedOutputHeight,
            int expectedOffsetX,
            int expectedOffsetY)
        {
            NativePixelViewport viewport = NativePixelViewportMath.Calculate(physicalWidth, physicalHeight);

            Assert.That(viewport.IntegerScale, Is.EqualTo(expectedScale));
            Assert.That(viewport.LogicalWidth, Is.EqualTo(expectedLogicalWidth));
            Assert.That(viewport.LogicalHeight, Is.EqualTo(expectedLogicalHeight));
            Assert.That(viewport.OutputWidth, Is.EqualTo(expectedOutputWidth));
            Assert.That(viewport.OutputHeight, Is.EqualTo(expectedOutputHeight));
            Assert.That(viewport.OutputOffsetX, Is.EqualTo(expectedOffsetX));
            Assert.That(viewport.OutputOffsetY, Is.EqualTo(expectedOffsetY));
        }

        [TestCase(1, 1)]
        [TestCase(127, 2048)]
        [TestCase(1071, 1073)]
        [TestCase(2143, 2144)]
        [TestCase(3217, 8192)]
        [TestCase(16384, 9000)]
        public void ValidPhysicalSize_AlwaysSatisfiesNativePixelInvariants(int width, int height)
        {
            NativePixelViewport viewport = NativePixelViewportMath.Calculate(width, height);

            Assert.That(viewport.IntegerScale, Is.GreaterThanOrEqualTo(1));
            Assert.That(viewport.LogicalWidth, Is.InRange(1, NativePixelViewportMath.MaximumLogicalDimension));
            Assert.That(viewport.LogicalHeight, Is.InRange(1, NativePixelViewportMath.MaximumLogicalDimension));
            Assert.That(viewport.OutputWidth, Is.LessThanOrEqualTo(width));
            Assert.That(viewport.OutputHeight, Is.LessThanOrEqualTo(height));
            Assert.That(viewport.OutputOffsetX, Is.EqualTo((width - viewport.OutputWidth) / 2));
            Assert.That(viewport.OutputOffsetY, Is.EqualTo((height - viewport.OutputHeight) / 2));
            Assert.That(viewport.OutputRect.xMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(viewport.OutputRect.yMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(viewport.OutputRect.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(viewport.OutputRect.yMax, Is.LessThanOrEqualTo(height));
        }

        [Test]
        public void PresentationConstants_PreserveApprovedPixelGeometry()
        {
            Assert.That(NativePixelPresentation.PixelsPerUnit, Is.EqualTo(16));
            Assert.That(NativePixelViewportMath.MaximumLogicalDimension, Is.EqualTo(1072));
            Assert.That(NativePixelPresentation.FullyVisibleRadiusPixels, Is.EqualTo(456));
            Assert.That(NativePixelPresentation.PenumbraThicknessPixels, Is.EqualTo(64));
            Assert.That(NativePixelPresentation.FullDarknessRadiusPixels, Is.EqualTo(520));
            Assert.That(NativePixelPresentation.WorldTargetDepthBits, Is.EqualTo(16));
            Assert.That(NativePixelPresentation.ResolvedTargetDepthBits, Is.EqualTo(0));
            Assert.That(
                NativePixelPresentation.FullyVisibleRadiusPixels +
                NativePixelPresentation.PenumbraThicknessPixels,
                Is.EqualTo(NativePixelPresentation.FullDarknessRadiusPixels));
        }

        [Test]
        public void UniversalRpAsset_PrunesUnusedThreeDimensionalCapabilities()
        {
            UniversalRenderPipelineAsset asset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                    "Assets/Settings/UniversalRP.asset");

            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.supportsCameraDepthTexture, Is.False);
            Assert.That(asset.supportsCameraOpaqueTexture, Is.False);
            Assert.That(asset.supportsHDR, Is.False);
            Assert.That(asset.supportsTerrainHoles, Is.False);
            Assert.That(asset.enableLODCrossFade, Is.False);
            Assert.That(asset.mainLightRenderingMode, Is.EqualTo(LightRenderingMode.Disabled));
            Assert.That(asset.additionalLightsRenderingMode, Is.EqualTo(LightRenderingMode.Disabled));
            Assert.That(asset.supportsMainLightShadows, Is.False);
            Assert.That(asset.supportsAdditionalLightShadows, Is.False);
            Assert.That(asset.supportsMixedLighting, Is.False);
            Assert.That(asset.supportsLightCookies, Is.False);
            Assert.That(asset.supportDataDrivenLensFlare, Is.False);
            Assert.That(asset.supportScreenSpaceLensFlare, Is.False);
            Assert.That(asset.useAdaptivePerformance, Is.False);
            Assert.That(asset.volumeFrameworkUpdateMode, Is.EqualTo(VolumeFrameworkUpdateMode.ViaScripting));
        }
    }
}
