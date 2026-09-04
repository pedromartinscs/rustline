using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rustline.Tests
{
    public sealed class LongwatchAimTests
    {
        private const string LongwatchIdleRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim/Idle";
        private const string LongwatchRunRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim/Run";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";

        private static readonly string[] DirectionSuffixes =
        {
            "p90", "p80", "p70", "p60", "p50", "p40", "p30", "p20", "p10", "0",
            "m10", "m20", "m30", "m40", "m50", "m60", "m70", "m80", "m90",
        };

        private static readonly int[] DirectionAngles =
        {
            90, 80, 70, 60, 50, 40, 30, 20, 10, 0,
            -10, -20, -30, -40, -50, -60, -70, -80, -90,
        };

        [TestCase(0f, 0)]
        [TestCase(4.99f, 0)]
        [TestCase(5f, 10)]
        [TestCase(44f, 40)]
        [TestCase(45f, 50)]
        [TestCase(89f, 90)]
        [TestCase(90f, 90)]
        [TestCase(-4.99f, 0)]
        [TestCase(-5f, -10)]
        [TestCase(-44f, -40)]
        [TestCase(-45f, -50)]
        [TestCase(-89f, -90)]
        [TestCase(-90f, -90)]
        public void NearestTenDegreeSelection_IsDeterministic(float continuousAngle, int expected)
        {
            Assert.That(LongwatchAimMath.QuantizeToNearestTen(continuousAngle), Is.EqualTo(expected));
        }

        [TestCase(1f, 0f, 0, false)]
        [TestCase(1f, 1f, 50, false)]
        [TestCase(1f, -1f, -50, false)]
        [TestCase(-1f, 0f, 0, true)]
        [TestCase(-1f, 1f, 50, true)]
        [TestCase(-1f, -1f, -50, true)]
        [TestCase(0f, 1f, 90, false)]
        [TestCase(0f, -1f, -90, false)]
        public void AimVector_NormalizesToRightAuthoredHemisphere(
            float x,
            float y,
            int expectedAngle,
            bool expectedFlip)
        {
            bool valid = LongwatchAimMath.TrySelect(
                new Vector2(x, y),
                false,
                LongwatchAimSelection.Default,
                out LongwatchAimSelection selection);

            Assert.That(valid, Is.True);
            Assert.That(selection.AuthoredAngleDegrees, Is.EqualTo(expectedAngle));
            Assert.That(selection.FlipX, Is.EqualTo(expectedFlip));
            Assert.That(selection.DirectionIndex, Is.EqualTo((90 - expectedAngle) / 10));
        }

        [Test]
        public void ContinuousAimAngle_RemainsUnquantizedAlongsideVisualPose()
        {
            float angle = 37f;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad));

            Assert.That(LongwatchAimMath.TrySelect(
                direction,
                false,
                LongwatchAimSelection.Default,
                out LongwatchAimSelection selection), Is.True);
            Assert.That(selection.ContinuousAngleDegrees, Is.EqualTo(angle).Within(0.0001f));
            Assert.That(selection.AuthoredAngleDegrees, Is.EqualTo(40));
        }

        [Test]
        public void ZeroLengthAim_RetainsPriorValidSelection()
        {
            LongwatchAimSelection previous = new LongwatchAimSelection(63.5f, 60, true);
            bool valid = LongwatchAimMath.TrySelect(
                Vector2.zero,
                true,
                previous,
                out LongwatchAimSelection selection);

            Assert.That(valid, Is.False);
            Assert.That(selection.ContinuousAngleDegrees, Is.EqualTo(previous.ContinuousAngleDegrees));
            Assert.That(selection.AuthoredAngleDegrees, Is.EqualTo(previous.AuthoredAngleDegrees));
            Assert.That(selection.FlipX, Is.EqualTo(previous.FlipX));
        }

        [Test]
        public void ExactVerticalAim_RetainsPriorFacingHemisphere()
        {
            LongwatchAimSelection previous = new LongwatchAimSelection(40f, 40, true);
            Assert.That(LongwatchAimMath.TrySelect(
                Vector2.up,
                true,
                previous,
                out LongwatchAimSelection selection), Is.True);
            Assert.That(selection.AuthoredAngleDegrees, Is.EqualTo(90));
            Assert.That(selection.FlipX, Is.True);
        }

        [Test]
        public void PhysicalPointerMapping_HandlesOneAndTwoTimesScale()
        {
            NativePixelViewport oneX = NativePixelViewportMath.Calculate(800, 600);
            Vector2 oneXCenter = NativePixelViewportMath.PhysicalToLogicalViewport(
                new Vector2(400f, 300f),
                oneX);
            Assert.That(oneXCenter, Is.EqualTo(new Vector2(0.5f, 0.5f)));

            NativePixelViewport twoX = NativePixelViewportMath.Calculate(3840, 2160);
            Vector2 physicalCenter = new Vector2(
                twoX.OutputOffsetX + twoX.OutputWidth * 0.5f,
                twoX.OutputOffsetY + twoX.OutputHeight * 0.5f);
            Vector2 twoXCenter = NativePixelViewportMath.PhysicalToLogicalViewport(
                physicalCenter,
                twoX);
            Assert.That(twoXCenter, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void PhysicalPointerMapping_AccountsForMarginsWithoutClamping()
        {
            NativePixelViewport viewport = NativePixelViewportMath.Calculate(1920, 1080);
            Assert.That(viewport.OutputOffsetX, Is.EqualTo(424));
            Assert.That(viewport.OutputOffsetY, Is.EqualTo(4));

            Vector2 logicalBottomLeft = NativePixelViewportMath.PhysicalToLogicalViewport(
                new Vector2(424f, 4f),
                viewport);
            Assert.That(logicalBottomLeft, Is.EqualTo(Vector2.zero));

            Vector2 deepSpacePointer = NativePixelViewportMath.PhysicalToLogicalViewport(
                Vector2.zero,
                viewport);
            Assert.That(deepSpacePointer.x, Is.LessThan(0f));
            Assert.That(deepSpacePointer.y, Is.LessThan(0f));
            Assert.That(deepSpacePointer.x, Is.EqualTo(-424f / 1072f).Within(0.000001f));
            Assert.That(deepSpacePointer.y, Is.EqualTo(-4f / 1072f).Within(0.000001f));
        }

        [Test]
        public void AllLongwatchIdleSheets_MatchAuthoredImportContract()
        {
            string[] importedGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { LongwatchIdleRoot });
            Assert.That(importedGuids, Has.Length.EqualTo(19),
                "Longwatch Idle folder must contain only the expected 19 direction textures.");

            for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
            {
                string baseName = "player_salvager_longwatch_dmr_idle_aim_" +
                    DirectionSuffixes[directionIndex];
                string path = LongwatchIdleRoot + "/" + baseName + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, "Missing direction sheet: " + path);
                Assert.That(texture.width, Is.EqualTo(160), path);
                Assert.That(texture.height, Is.EqualTo(96), path);

                AssertLongwatchImporter(path);
                List<Sprite> sprites = LoadSprites(path);
                Assert.That(sprites, Has.Count.EqualTo(2), path);
                for (int frameIndex = 0; frameIndex < 2; frameIndex++)
                {
                    Sprite sprite = sprites[frameIndex];
                    Assert.That(sprite.name, Is.EqualTo(baseName + "_" + frameIndex));
                    Assert.That(sprite.rect, Is.EqualTo(new Rect(frameIndex * 80, 0, 80, 96)));
                    Assert.That(Vector2.Distance(sprite.pivot, new Vector2(24f, 8f)),
                        Is.LessThan(0.001f));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16f));
                }

                AssertSourcePixels(path);
            }
        }

        [Test]
        public void AllLongwatchRunSheets_MatchAuthoredImportContract()
        {
            string[] importedGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { LongwatchRunRoot });
            Assert.That(importedGuids, Has.Length.EqualTo(19),
                "Longwatch Run folder must contain only the expected 19 direction textures.");

            int totalSprites = 0;
            for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
            {
                string baseName = "player_salvager_longwatch_dmr_run_aim_" +
                    DirectionSuffixes[directionIndex];
                string path = LongwatchRunRoot + "/" + baseName + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, "Missing direction sheet: " + path);
                Assert.That(texture.width, Is.EqualTo(480), path);
                Assert.That(texture.height, Is.EqualTo(96), path);

                AssertLongwatchImporter(path);
                List<Sprite> sprites = LoadSprites(path);
                Assert.That(sprites, Has.Count.EqualTo(6), path);
                totalSprites += sprites.Count;
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    Sprite sprite = sprites[frameIndex];
                    Assert.That(sprite.name, Is.EqualTo(baseName + "_" + frameIndex));
                    Assert.That(sprite.rect, Is.EqualTo(new Rect(frameIndex * 80, 0, 80, 96)));
                    Assert.That(Vector2.Distance(sprite.pivot, new Vector2(24f, 8f)),
                        Is.LessThan(0.001f));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16f));
                }

                AssertSourcePixels(path);
            }

            Assert.That(totalSprites, Is.EqualTo(114));
        }

        [Test]
        public void PlayerPrefab_ContainsCompleteLongwatchPoseMapping()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            PlayerLongwatchAimPresenter2D presenter =
                prefab?.GetComponent<PlayerLongwatchAimPresenter2D>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.BodyIdleFrameCount, Is.EqualTo(2));
            Assert.That(presenter.IdleAimPoseCount, Is.EqualTo(19));
            Assert.That(presenter.BodyRunFrameCount, Is.EqualTo(6));
            Assert.That(presenter.RunAimPoseCount, Is.EqualTo(19));
            Assert.That(presenter.NativePixelPresentation, Is.Null,
                "The scene-only presentation dependency must not be forged inside the prefab.");
            Assert.That(PlayerLongwatchAimPresenter2D.AimOriginOffsetSourcePixels, Is.EqualTo(38f));
            Assert.That(PlayerLongwatchAimPresenter2D.AimOriginOffsetWorldUnits, Is.EqualTo(2.375f));
            Assert.That(presenter.AimOriginWorld - presenter.BodySpriteRenderer.transform.position,
                Is.EqualTo(Vector3.up * 2.375f));

            HashSet<Sprite> mappedIdleSprites = new HashSet<Sprite>();
            HashSet<Sprite> mappedRunSprites = new HashSet<Sprite>();
            for (int index = 0; index < DirectionAngles.Length; index++)
            {
                LongwatchIdleAimPose idlePose = presenter.GetIdleAimPose(index);
                Assert.That(idlePose.AngleDegrees, Is.EqualTo(DirectionAngles[index]));
                Assert.That(idlePose.Frame0.name, Does.EndWith("_" + DirectionSuffixes[index] + "_0"));
                Assert.That(idlePose.Frame1.name, Does.EndWith("_" + DirectionSuffixes[index] + "_1"));
                Assert.That(mappedIdleSprites.Add(idlePose.Frame0), Is.True);
                Assert.That(mappedIdleSprites.Add(idlePose.Frame1), Is.True);

                LongwatchRunAimPose runPose = presenter.GetRunAimPose(index);
                Assert.That(runPose.AngleDegrees, Is.EqualTo(DirectionAngles[index]));
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    Sprite runFrame = runPose.GetFrame(frameIndex);
                    Assert.That(runFrame.name,
                        Does.EndWith("_" + DirectionSuffixes[index] + "_" + frameIndex));
                    Assert.That(mappedRunSprites.Add(runFrame), Is.True);
                }
            }

            Assert.That(mappedIdleSprites, Has.Count.EqualTo(38));
            Assert.That(mappedRunSprites, Has.Count.EqualTo(114));
        }

        [Test]
        public void PlayerInput_UsesPointerPositionPassThroughAction()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputActionMap player = actions?.FindActionMap("Player", false);
            InputAction pointer = player?.FindAction("PointerPosition", false);
            Assert.That(pointer, Is.Not.Null);
            Assert.That(pointer.type, Is.EqualTo(InputActionType.PassThrough));
            Assert.That(pointer.expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(pointer.bindings, Has.Count.EqualTo(1));
            Assert.That(pointer.bindings[0].path, Is.EqualTo("<Pointer>/position"));
        }

        private static void AssertLongwatchImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.crunchedCompression, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);
        }

        private static List<Sprite> LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)))
                .ToList();
        }

        private static void AssertSourcePixels(string path)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.That(source.LoadImage(File.ReadAllBytes(Path.Combine(projectRoot, path)), false), Is.True);
                Color32[] pixels = source.GetPixels32();
                for (int index = 0; index < pixels.Length; index++)
                {
                    Color32 pixel = pixels[index];
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True,
                        path + " contains partial alpha at pixel " + index + ".");
                    if (pixel.a == 255)
                    {
                        Assert.That(RustlinePalette.IsCanonical(pixel), Is.True,
                            path + " contains a non-canonical opaque pixel at " + index + ".");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }
    }
}
