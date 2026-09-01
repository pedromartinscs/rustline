using System.Collections;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;
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
            AssertTarget(presentation.ResolvedTarget, expected, requiresCameraDepth: false);

            RenderTexture originalWorldTarget = presentation.WorldTarget;
            RenderTexture originalResolvedTarget = presentation.ResolvedTarget;
            presentation.TogglePenumbra();
            yield return null;

            Assert.That(presentation.PenumbraEnabled, Is.False);
            Assert.That(presentation.WorldTarget, Is.SameAs(originalWorldTarget));
            Assert.That(presentation.ResolvedTarget, Is.SameAs(originalResolvedTarget));
            presentation.TogglePenumbra();
            Assert.That(presentation.PenumbraEnabled, Is.True);
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
    }
}
