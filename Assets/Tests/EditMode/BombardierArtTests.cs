using System.IO;
using System.Linq;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class BombardierArtTests
    {
        private const string Sheet = "Assets/Art/Characters/Enemies/Bombardier/bombardier_demolition_sheet.png";

        [TestCase(BombardierState.Patrol, 0f, 0)]
        [TestCase(BombardierState.Windup, 0.7f, 6)]
        [TestCase(BombardierState.Windup, 0.4f, 7)]
        [TestCase(BombardierState.Windup, 0.01f, 8)]
        [TestCase(BombardierState.Recover, 1.3f, 9)]
        [TestCase(BombardierState.Recover, 1.1f, 10)]
        public void CombatFrames_FollowAuthoritativeTimer(BombardierState state, float remaining, int expected)
        {
            Assert.That(Bombardier2D.WindupDuration, Is.EqualTo(0.7f));
            Assert.That(Bombardier2D.RecoveryDuration, Is.EqualTo(1.3f));
            Assert.That(BombardierAnimationFrames.Select(state, true, false, false, remaining, 0, 0), Is.EqualTo(expected));
        }

        [Test]
        public void DormancyHitAndDeath_HaveExplicitPrecedence()
        {
            Assert.That(BombardierAnimationFrames.Select(BombardierState.Patrol, false, true, false, 0, 0, 0), Is.Zero);
            Assert.That(BombardierAnimationFrames.Select(BombardierState.Patrol, false, false, false, 0, 0.6f, 0), Is.EqualTo(1));
            Assert.That(BombardierAnimationFrames.Select(BombardierState.Windup, true, false, true, 0.5f, 0, 0), Is.EqualTo(11));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(BombardierAnimationFrames.Select(BombardierState.Dead, false, false, true, 0, 0, i * 0.14f + 0.01f), Is.EqualTo(12 + i));
                Assert.That(BombardierAnimationFrames.Select(BombardierState.Patrol, true, true, false, 0, i / 8f, 0), Is.EqualTo(2 + i));
            }
            Assert.That(BombardierAnimationFrames.Select(BombardierState.Dead, true, true, true, 0, 0, 100), Is.EqualTo(-1));
        }

        [Test]
        public void ProductionSheet_HasCanonicalBinaryPixelsFixedPivotsAndStableSubassets()
        {
            const string source = "Assets/Art/Source/Enemies/Bombardier/bombardier_generated_source.png";
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(source)).textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>(), Is.Empty);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled || importer.crunchedCompression, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            Assert.That(sprites.Length, Is.EqualTo(16));
            var ids = new System.Collections.Generic.Dictionary<string, long>();
            foreach (Sprite sprite in sprites)
            {
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(48, 64)));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(24, 0)));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16));
                Assert.That(sprite.GetPhysicsShapeCount(), Is.Zero);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id), Is.True);
                ids.Add(sprite.name, id);
            }
            Assert.That(ids.Values.Distinct().Count(), Is.EqualTo(16));
            AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>())
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id);
                Assert.That(id, Is.EqualTo(ids[sprite.name]), "Sprite ID changed after import.");
            }
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                raw.LoadImage(File.ReadAllBytes(Sheet));
                Assert.That(raw.width, Is.EqualTo(192)); Assert.That(raw.height, Is.EqualTo(256));
                Color32[] pixels = raw.GetPixels32();
                foreach (Color32 pixel in pixels)
                {
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True, "Nonbinary alpha.");
                    if (pixel.a != 0) Assert.That(RustlinePalette.IsCanonical(pixel), Is.True, "Noncanonical Bombardier pixel.");
                }
                for (int frame = 0; frame < 16; frame++)
                {
                    int count = 0;
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 48; x++)
                            if (pixels[((3 - frame / 4) * 64 + y) * 192 + frame % 4 * 48 + x].a > 0) count++;
                    Assert.That(count, Is.GreaterThan(150), "Empty/fragmentary pose: " + frame);
                }
            }
            finally { Object.DestroyImmediate(raw); }
        }
    }
}
