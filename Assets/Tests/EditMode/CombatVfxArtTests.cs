using System.IO;
using System.Linq;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class CombatVfxArtTests
    {
        private const string Explosion = "Assets/Art/Effects/Explosions/Bombardier/bombardier_explosion.png";
        private const string Dissolve = "Assets/Art/Characters/Enemies/Bombardier/bombardier_dissolution.png";

        [TestCase(0f, 0)] [TestCase(0.061f, 1)] [TestCase(0.121f, 2)]
        [TestCase(0.181f, 3)] [TestCase(0.241f, 4)] [TestCase(0.301f, 5)] [TestCase(0.361f, -1)]
        public void Explosion_SelectsSixOrderedKeysThenFinishes(float age, int expected)
        {
            Assert.That(BombardierExplosionPresenter2D.Duration, Is.EqualTo(0.36f).Within(0.0001f));
            Assert.That(BombardierExplosionPresenter2D.SelectFrame(age), Is.EqualTo(expected));
            Assert.That(BombardierBallistics2D.ExplosionRadius, Is.EqualTo(1.75f));
            Assert.That(BombardierBallistics2D.ExplosionDamage, Is.EqualTo(25));
        }

        [TestCase(0f, 12)] [TestCase(0.15f, 13)] [TestCase(0.29f, 14)] [TestCase(0.43f, 15)]
        [TestCase(0.57f, 15)] [TestCase(1.40f, 15)] [TestCase(1.46f, 16)]
        [TestCase(1.59f, 17)] [TestCase(1.72f, 18)] [TestCase(1.81f, -1)]
        public void Death_RetainsOriginalCollapseThenHoldDissolveAndHidden(float age, int frame) =>
            Assert.That(BombardierAnimationFrames.DeathFrame(age), Is.EqualTo(frame));

        [TestCase(Explosion, 6, 64, 32)]
        [TestCase(Dissolve, 3, 48, 0)]
        public void ProductionVfx_UsesCanonicalBinaryPixelsCorrectImportsAndStableIDs(string path, int count, int width, int pivotY)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled || importer.crunchedCompression, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            Assert.That(sprites.Length, Is.EqualTo(count));
            var ids = new System.Collections.Generic.Dictionary<string, long>();
            foreach (var sprite in sprites)
            {
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(width, 64)));
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(width / 2, pivotY)));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16));
                Assert.That(sprite.GetPhysicsShapeCount(), Is.Zero);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id), Is.True);
                ids.Add(sprite.name, id);
            }
            Assert.That(ids.Values.Distinct().Count(), Is.EqualTo(count));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id);
                Assert.That(id, Is.EqualTo(ids[sprite.name]));
            }
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                raw.LoadImage(File.ReadAllBytes(path));
                foreach (Color32 pixel in raw.GetPixels32())
                {
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True);
                    if (pixel.a != 0) Assert.That(RustlinePalette.IsCanonical(pixel), Is.True);
                }
            }
            finally { Object.DestroyImmediate(raw); }
        }

        [Test]
        public void Explosion_OpaquePixelsFitRealDamageRadiusAndExpansionIsAuthored()
        {
            const string source = "Assets/Art/Source/Effects/Bombardier/explosion_generated_source.png";
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(source)).textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>(), Is.Empty);
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                raw.LoadImage(File.ReadAllBytes(Explosion));
                Assert.That(raw.width, Is.EqualTo(192)); Assert.That(raw.height, Is.EqualTo(128));
                Color32[] pixels = raw.GetPixels32(); int flashCount = 0;
                for (int frame = 0; frame < 6; frame++)
                {
                    int count = 0; float radius = 0f;
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 64; x++)
                        {
                            if (pixels[((1 - frame / 3) * 64 + y) * 192 + frame % 3 * 64 + x].a == 0) continue;
                            count++;
                            float distance = new Vector2(x + 0.5f - 32f, y + 0.5f - 32f).magnitude;
                            radius = Mathf.Max(radius, distance);
                            Assert.That(distance / 16f, Is.LessThanOrEqualTo(BombardierBallistics2D.ExplosionRadius));
                        }
                    Assert.That(count, Is.GreaterThan(30), "Empty/fragmentary effect key.");
                    if (frame == 0) flashCount = count;
                    if (frame == 2)
                    {
                        Assert.That(radius, Is.GreaterThan(24f));
                        Assert.That(count, Is.GreaterThan(flashCount * 2));
                    }
                }
            }
            finally { Object.DestroyImmediate(raw); }
        }

        [Test]
        public void Dissolution_IsProgressiveExactColorSubsetOfApprovedWreck()
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                original.LoadImage(File.ReadAllBytes("Assets/Art/Characters/Enemies/Bombardier/bombardier_demolition_sheet.png"));
                mask.LoadImage(File.ReadAllBytes(Dissolve));
                Color32[] source = original.GetPixels32(), pixels = mask.GetPixels32();
                int previous = int.MaxValue;
                for (int frame = 0; frame < 3; frame++)
                {
                    int count = 0;
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 48; x++)
                        {
                            Color32 pixel = pixels[y * 144 + frame * 48 + x];
                            if (pixel.a == 0) continue;
                            count++;
                            Assert.That(pixel, Is.EqualTo(source[y * 192 + 144 + x]));
                            if (frame > 0) Assert.That(pixel, Is.EqualTo(pixels[y * 144 + (frame - 1) * 48 + x]));
                        }
                    Assert.That(count, Is.InRange(1, previous - 1)); previous = count;
                }
            }
            finally { Object.DestroyImmediate(original); Object.DestroyImmediate(mask); }
        }
    }
}
