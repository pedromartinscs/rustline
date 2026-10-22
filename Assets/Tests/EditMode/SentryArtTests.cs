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

        [TestCase(SentryState.Alert, 0.35f, 6)]
        [TestCase(SentryState.Windup, 0.55f, 7)]
        [TestCase(SentryState.Windup, 0.27f, 8)]
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
            Assert.That(SentryAnimationFrames.Select(SentryState.Dead, false, false, false, 0, 0, 100), Is.EqualTo(15));
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
