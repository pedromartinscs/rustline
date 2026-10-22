using System.IO;
using System.Linq;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class SentryArtTests
    {
        private const string Sheet = "Assets/Art/Characters/Enemies/Sentry/sentry_security_sheet.png";

        [Test]
        public void Windup_CommitsOnceAtShorterAuthoritativeEdge()
        {
            Assert.That(SentryBrain2D.WindupDuration, Is.EqualTo(0.48f));
            var brain = new SentryBrain2D();
            brain.Step(0f, true, true);
            brain.Step(SentryBrain2D.AlertDuration, true, true);
            brain.Step(0f, true, true);
            Assert.That(brain.State, Is.EqualTo(SentryState.Windup));
            Assert.That(brain.Step(0.47f, true, true), Is.False);
            Assert.That(SentryAnimationFrames.Select(brain.State, true, false, false, brain.Remaining, 0, 0), Is.EqualTo(8));
            Assert.That(brain.Step(0.011f, true, true), Is.True);
            Assert.That(SentryAnimationFrames.Select(brain.State, true, false, false, brain.Remaining, 0, 0), Is.EqualTo(9));
            Assert.That(brain.Step(0.1f, true, true), Is.False);
        }

        [TestCase(0f, 12)]
        [TestCase(0.13f, 13)]
        [TestCase(0.25f, 14)]
        [TestCase(0.37f, 15)]
        [TestCase(1.07f, 15)]
        [TestCase(1.09f, 16)]
        [TestCase(1.22f, 17)]
        [TestCase(1.35f, 18)]
        [TestCase(1.48f, -1)]
        public void Death_PreservesCollapseHoldsWreckThenDisintegrates(float elapsed, int frame) =>
            Assert.That(SentryAnimationFrames.DeathFrame(elapsed), Is.EqualTo(frame));

        [Test]
        public void Dissolution_OnlyRemovesOriginalPixelsInProgressiveBinaryMasks()
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var dissolve = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                original.LoadImage(File.ReadAllBytes(Sheet));
                const string path = "Assets/Art/Characters/Enemies/Sentry/sentry_dissolution.png";
                dissolve.LoadImage(File.ReadAllBytes(path));
                var pixels = dissolve.GetPixels32();
                var source = original.GetPixels32();
                int previous = int.MaxValue;
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count(), Is.EqualTo(3));
                for (int frame = 0; frame < 3; frame++)
                {
                    int count = 0;
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 48; x++)
                        {
                            Color32 pixel = pixels[y * 144 + frame * 48 + x];
                            Assert.That(pixel.a == 0 || pixel.a == 255, Is.True);
                            if (pixel.a == 0) continue;
                            count++;
                            Assert.That(pixel, Is.EqualTo(source[y * 192 + 144 + x]));
                            if (frame > 0) Assert.That(pixel, Is.EqualTo(pixels[y * 144 + (frame - 1) * 48 + x]));
                        }
                    Assert.That(count, Is.InRange(1, previous - 1));
                    previous = count;
                }
            }
            finally { Object.DestroyImmediate(original); Object.DestroyImmediate(dissolve); }
        }

        [TestCase(SentryState.Alert, 0.35f, 6)]
        [TestCase(SentryState.Windup, 0.48f, 7)]
        [TestCase(SentryState.Windup, 0.23f, 8)]
        [TestCase(SentryState.Windup, 0.001f, 8)]
        [TestCase(SentryState.Recovery, 0.9f, 9)]
        [TestCase(SentryState.Recovery, 0.7f, 10)]
        public void Frames_UseAuthoritativeBrainTimer(SentryState state, float remaining, int expected) =>
            Assert.That(SentryAnimationFrames.Select(state, true, false, false, remaining, 500f, 0f), Is.EqualTo(expected));

        [Test]
        public void DormancyHitAndDeath_HaveExplicitPrecedenceAndClampedDeath()
        {
            Assert.That(SentryAnimationFrames.Select(SentryState.Patrol, false, true, false, 0, 0, 0), Is.Zero);
            Assert.That(SentryAnimationFrames.Select(SentryState.Patrol, false, false, false, 0, 0.6f, 0), Is.EqualTo(1));
            Assert.That(SentryAnimationFrames.Select(SentryState.Windup, true, false, true, 0.5f, 0, 0), Is.EqualTo(11));
            Assert.That(SentryAnimationFrames.Select(SentryState.Dead, false, false, true, 0, 0, 0), Is.EqualTo(12));
            Assert.That(SentryAnimationFrames.Select(SentryState.Dead, false, false, false, 0, 0, 100), Is.EqualTo(-1));
            for (int i = 0; i < 4; i++)
                Assert.That(SentryAnimationFrames.Select(SentryState.Patrol, true, true, false, 0, i / 8f, 0), Is.EqualTo(i + 2));
        }

        [Test]
        public void ProductionSheet_HasCanonicalPixelsBinaryAlphaAndCompleteUnscaledCells()
        {
            const string sourcePath = "Assets/Art/Source/Enemies/Sentry/sentry_generated_source.png";
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(sourcePath)).textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Sprite>(), Is.Empty,
                "Noncanonical reference art must not be exposed as gameplay sprites.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled || importer.crunchedCompression, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToArray();
            Assert.That(sprites.Length, Is.EqualTo(16));
            foreach (Sprite sprite in sprites)
            {
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(48, 64)));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(24, 0)));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16));
                Assert.That(sprite.GetPhysicsShapeCount(), Is.Zero);
            }
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                raw.LoadImage(File.ReadAllBytes(Sheet));
                Assert.That(raw.width, Is.EqualTo(192));
                Assert.That(raw.height, Is.EqualTo(256));
                Color32[] pixels = raw.GetPixels32();
                foreach (Color32 pixel in pixels)
                {
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True, "Partial alpha in production art.");
                    if (pixel.a != 0) Assert.That(RustlinePalette.IsCanonical(pixel), Is.True, "Noncanonical Sentry pixel.");
                }
                for (int frame = 0; frame < 16; frame++)
                {
                    int count = 0;
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 48; x++)
                            if (pixels[((3 - frame / 4) * 64 + y) * 192 + frame % 4 * 48 + x].a > 0) count++;
                    Assert.That(count, Is.GreaterThan(150), "Empty/fragmentary Sentry pose: " + frame);
                }
            }
            finally { Object.DestroyImmediate(raw); }
        }
    }
}
