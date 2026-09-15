using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class Latch9MuzzleFlashTests
    {
        private const string ConventionalFlashPath =
            "Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash.png";
        private const string BouncingFlashPath =
            "Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash_bouncing.png";
        private const string MetadataPath =
            "Assets/Config/Weapons/Generated/Latch9MuzzleMetadata.asset";

        [Test]
        public void MuzzleFlashSheets_MatchExactImportAndPaletteContracts()
        {
            AssertFlashSheet(
                ConventionalFlashPath,
                "latch_9_muzzle_flash",
                new HashSet<Color32>
                {
                    RustlinePalette.GetColor(18),
                    RustlinePalette.GetColor(19),
                    RustlinePalette.GetColor(20),
                    RustlinePalette.GetColor(24),
                });

            AssertFlashSheet(
                BouncingFlashPath,
                "latch_9_muzzle_flash_bouncing",
                new HashSet<Color32>
                {
                    RustlinePalette.GetColor(22),
                    RustlinePalette.GetColor(24),
                    RustlinePalette.GetColor(27),
                });
        }

        [Test]
        public void RuntimeMetadata_ContainsAll361SupportedLatchAimPoints()
        {
            Latch9MuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<Latch9MuzzleMetadata2D>(MetadataPath);
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.SchemaVersion, Is.EqualTo(1));
            Assert.That(metadata.GeneratorVersion, Is.EqualTo(2));
            Assert.That(metadata.WeaponId, Is.EqualTo("latch_9"));
            Assert.That(metadata.CellSizePixels, Is.EqualTo(new Vector2Int(80, 96)));
            Assert.That(metadata.PivotPixels, Is.EqualTo(new Vector2Int(24, 8)));
            Assert.That(metadata.GetSupportedPointCount(), Is.EqualTo(361));

            Latch9MuzzleState2D[] states =
            {
                Latch9MuzzleState2D.Idle,
                Latch9MuzzleState2D.Run,
                Latch9MuzzleState2D.Backpedal,
                Latch9MuzzleState2D.Crouch,
                Latch9MuzzleState2D.Fall,
            };
            int[] frameCounts = { 2, 6, 4, 6, 1 };

            for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                for (int directionIndex = 0;
                     directionIndex < Latch9MuzzleMetadata2D.DirectionCount;
                     directionIndex++)
                {
                    Latch9MuzzleDirection2D direction =
                        metadata.GetDirection(states[stateIndex], directionIndex);
                    Assert.That(direction.Supported, Is.True);
                    Assert.That(direction.FrameCount, Is.EqualTo(frameCounts[stateIndex]));
                    for (int frameIndex = 0; frameIndex < frameCounts[stateIndex]; frameIndex++)
                    {
                        Assert.That(metadata.TryGetMuzzleOffset(
                            states[stateIndex], directionIndex, frameIndex, out _), Is.True);
                    }
                }
            }

            Assert.That(metadata.TryGetMuzzleOffset(
                Latch9MuzzleState2D.Idle, 0, 0, out Vector2 p90Frame0), Is.True);
            Assert.That(p90Frame0, Is.EqualTo(new Vector2(-7.5f, 69.5f)));
            Assert.That(metadata.TryGetMuzzleOffset(
                Latch9MuzzleState2D.Idle, 0, 1, out Vector2 p90Frame1), Is.True);
            Assert.That(p90Frame1, Is.EqualTo(new Vector2(-6.5f, 70.5f)));
        }

        [Test]
        public void PositionRotationVariantsAndProfile_FollowLatchContract()
        {
            Assert.That(Latch9MuzzleFlashPresenter2D.RequiredSpriteCount, Is.EqualTo(20));
            Assert.That(Latch9MuzzleFlashMath.ToLocalPosition(
                    new Vector2(40.5f, 41.5f), false),
                Is.EqualTo(new Vector2(40.5f / 16f, 41.5f / 16f)));
            Assert.That(Latch9MuzzleFlashMath.ToLocalPosition(
                    new Vector2(40.5f, 41.5f), true),
                Is.EqualTo(new Vector2(-40.5f / 16f, 41.5f / 16f)));
            Assert.That(Latch9MuzzleFlashMath.ToLocalAngle(30, false), Is.EqualTo(30f));
            Assert.That(Latch9MuzzleFlashMath.ToLocalAngle(30, true), Is.EqualTo(150f));
            Assert.That(Mathf.DeltaAngle(
                    0f, Latch9MuzzleFlashMath.ToLocalAngle(-30, true)),
                Is.EqualTo(-150f));

            var variants = new HashSet<int>();
            for (int sequence = 0; sequence < 10; sequence++)
            {
                variants.Add(Latch9MuzzleFlashMath.SelectVariant(sequence));
            }
            Assert.That(variants, Is.EquivalentTo(Enumerable.Range(0, 10)));

            GameObject root = new GameObject("Latch-9 Muzzle Profile Test");
            try
            {
                Latch9MuzzleFlashPresenter2D presenter =
                    root.AddComponent<Latch9MuzzleFlashPresenter2D>();
                Assert.That(presenter.Profile,
                    Is.EqualTo(Latch9MuzzleFlashProfile2D.Conventional));
                presenter.SetProfile(Latch9MuzzleFlashProfile2D.Bouncing);
                Assert.That(presenter.Profile,
                    Is.EqualTo(Latch9MuzzleFlashProfile2D.Bouncing));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertFlashSheet(
            string path,
            string prefix,
            HashSet<Color32> expectedColors)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(180));
            Assert.That(texture.height, Is.EqualTo(9));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.crunchedCompression, Is.False);
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            Assert.That(sprites, Has.Count.EqualTo(20));
            for (int variant = 0; variant < 10; variant++)
            {
                for (int frame = 0; frame < 2; frame++)
                {
                    int index = variant * 2 + frame;
                    string name = prefix + "_v" + variant.ToString("00") + "_f" + frame;
                    Assert.That(sprites.TryGetValue(name, out Sprite sprite), Is.True, name);
                    Assert.That(sprite.rect, Is.EqualTo(new Rect(index * 9, 0, 9, 9)));
                    Assert.That(sprite.pivot.x, Is.EqualTo(0.5f).Within(0.001f));
                    Assert.That(sprite.pivot.y, Is.EqualTo(4.5f).Within(0.001f));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16f));
                }
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.That(source.LoadImage(
                    File.ReadAllBytes(Path.Combine(projectRoot, path)), false), Is.True);
                var observedColors = new HashSet<Color32>();
                foreach (Color32 pixel in source.GetPixels32())
                {
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True);
                    if (pixel.a > 0)
                    {
                        Assert.That(expectedColors.Contains(pixel), Is.True);
                        observedColors.Add(pixel);
                    }
                }

                Assert.That(observedColors.SetEquals(expectedColors), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }
    }
}
