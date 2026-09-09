using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class LongwatchMuzzleFlashTests
    {
        private const string FlashPath =
            "Assets/Art/Effects/Weapons/longwatch_dmr/longwatch_dmr_muzzle_flash.png";
        private const string MetadataPath =
            "Assets/Config/Weapons/Generated/LongwatchDMRMuzzleMetadata.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

        [Test]
        public void MuzzleFlashSheet_MatchesExactImportAndPaletteContract()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FlashPath);
            TextureImporter importer = AssetImporter.GetAtPath(FlashPath) as TextureImporter;
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
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.crunchedCompression, Is.False);
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(FlashPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            Assert.That(sprites, Has.Count.EqualTo(20));
            for (int variant = 0; variant < 10; variant++)
            {
                for (int frame = 0; frame < 2; frame++)
                {
                    int index = variant * 2 + frame;
                    string name = "longwatch_dmr_muzzle_flash_v" + variant.ToString("00") +
                        "_f" + frame;
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
                    File.ReadAllBytes(Path.Combine(projectRoot, FlashPath)), false), Is.True);
                var expectedColors = new HashSet<Color32>
                {
                    RustlinePalette.GetColor(11),
                    RustlinePalette.GetColor(13),
                    RustlinePalette.GetColor(23),
                    RustlinePalette.GetColor(24),
                };
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

        [Test]
        public void RuntimeMetadata_ContainsExactGeneratedPointsAndUnsupportedCrouchSector()
        {
            LongwatchMuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<LongwatchMuzzleMetadata2D>(MetadataPath);
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.SchemaVersion, Is.EqualTo(1));
            Assert.That(metadata.GeneratorVersion, Is.EqualTo(1));
            Assert.That(metadata.WeaponId, Is.EqualTo("longwatch_dmr"));
            Assert.That(metadata.CellSizePixels, Is.EqualTo(new Vector2Int(80, 96)));
            Assert.That(metadata.PivotPixels, Is.EqualTo(new Vector2Int(24, 8)));
            Assert.That(metadata.GetSupportedPointCount(), Is.EqualTo(324));

            AssertOffset(metadata, LongwatchMuzzleState2D.Idle, 0, 0, new Vector2(-5.5f, 84.5f));
            AssertOffset(metadata, LongwatchMuzzleState2D.Idle, 9, 1, new Vector2(41.5f, 42.5f));
            AssertOffset(metadata, LongwatchMuzzleState2D.Run, 6, 4, new Vector2(41.5f, 66.5f));
            AssertOffset(metadata, LongwatchMuzzleState2D.Backpedal, 13, 3, new Vector2(27.5f, 15.5f));
            AssertOffset(metadata, LongwatchMuzzleState2D.Crouch, 15, 5, new Vector2(24.5f, -4.5f));

            for (int directionIndex = 16; directionIndex < 19; directionIndex++)
            {
                LongwatchMuzzleDirection2D direction =
                    metadata.GetDirection(LongwatchMuzzleState2D.Crouch, directionIndex);
                Assert.That(direction.Supported, Is.False);
                Assert.That(direction.FrameCount, Is.Zero);
                Assert.That(metadata.TryGetMuzzleOffset(
                    LongwatchMuzzleState2D.Crouch, directionIndex, 0, out _), Is.False);
            }
        }

        [Test]
        public void RenderedPoseResolver_UsesDisplayedBodyFrameAuthoredAngleAndFacing()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                PlayerLongwatchAimPresenter2D presenter =
                    instance.GetComponent<PlayerLongwatchAimPresenter2D>();
                PlayerAnimator2D animator = instance.GetComponent<PlayerAnimator2D>();
                PlayerAim2D aim = instance.GetComponent<PlayerAim2D>();
                SpriteRenderer armsRenderer = presenter.ArmsWeaponSpriteRenderer;
                typeof(PlayerLongwatchAimPresenter2D).GetMethod("OnEnable",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(presenter, null);

                AssertPose(presenter, animator, aim, armsRenderer,
                    PlayerAnimationState.Idle, presenter.GetBodyIdleFrame(1),
                    new Vector2(1f, 0f), LongwatchMuzzleState2D.Idle, 9, 0, 1, false);
                AssertPose(presenter, animator, aim, armsRenderer,
                    PlayerAnimationState.Run, presenter.GetBodyRunFrame(4),
                    Direction(30f, false), LongwatchMuzzleState2D.Run, 6, 30, 4, false);
                AssertPose(presenter, animator, aim, armsRenderer,
                    PlayerAnimationState.Backpedal, presenter.GetBodyBackpedalFrame(2),
                    Direction(30f, true), LongwatchMuzzleState2D.Backpedal, 6, 30, 2, true);
                AssertPose(presenter, animator, aim, armsRenderer,
                    PlayerAnimationState.CrouchIdle, presenter.GetBodyCrouchFrame(0),
                    Direction(-60f, false), LongwatchMuzzleState2D.Crouch, 15, -60, 0, false);
                AssertPose(presenter, animator, aim, armsRenderer,
                    PlayerAnimationState.CrouchMove, presenter.GetBodyCrouchFrame(5),
                    Direction(-20f, false), LongwatchMuzzleState2D.Crouch, 11, -20, 5, false);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PositionRotationAndVariants_FollowAuthoredPoseContract()
        {
            Assert.That(LongwatchMuzzleFlashMath.ToLocalPosition(
                new Vector2(-5.5f, 84.5f), false),
                Is.EqualTo(new Vector2(-5.5f / 16f, 84.5f / 16f)));
            Assert.That(LongwatchMuzzleFlashMath.ToLocalPosition(
                new Vector2(40.5f, 41.5f), true),
                Is.EqualTo(new Vector2(-40.5f / 16f, 41.5f / 16f)));
            Assert.That(LongwatchMuzzleFlashMath.ToLocalPosition(
                new Vector2(24.5f, -4.5f), true),
                Is.EqualTo(new Vector2(-24.5f / 16f, -4.5f / 16f)));

            Assert.That(LongwatchMuzzleFlashMath.ToLocalAngle(0, false), Is.EqualTo(0f));
            Assert.That(LongwatchMuzzleFlashMath.ToLocalAngle(0, true), Is.EqualTo(180f));
            Assert.That(LongwatchMuzzleFlashMath.ToLocalAngle(30, false), Is.EqualTo(30f));
            Assert.That(LongwatchMuzzleFlashMath.ToLocalAngle(30, true), Is.EqualTo(150f));
            Assert.That(LongwatchMuzzleFlashMath.ToLocalAngle(-30, false), Is.EqualTo(-30f));
            Assert.That(Mathf.DeltaAngle(0f,
                LongwatchMuzzleFlashMath.ToLocalAngle(-30, true)), Is.EqualTo(-150f));

            var variants = new HashSet<int>();
            for (int sequence = 0; sequence < 10; sequence++)
            {
                variants.Add(LongwatchMuzzleFlashMath.SelectVariant(sequence));
            }

            Assert.That(variants, Is.EquivalentTo(Enumerable.Range(0, 10)));
            Assert.That(LongwatchMuzzleFlashMath.SelectVariant(10),
                Is.EqualTo(LongwatchMuzzleFlashMath.SelectVariant(0)));
        }

        [Test]
        public void UnsupportedCrouchPose_FailsWithoutFabricatingFlashTransform()
        {
            LongwatchMuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<LongwatchMuzzleMetadata2D>(MetadataPath);
            for (int directionIndex = 16; directionIndex < 19; directionIndex++)
            {
                int angle = 90 - directionIndex * 10;
                var pose = new LongwatchRenderedPose2D(
                    LongwatchMuzzleState2D.Crouch, directionIndex, angle, 0, false);
                Assert.That(LongwatchMuzzleFlashMath.TryResolve(
                    metadata, in pose, out _, out _), Is.False);
            }
        }

        private static void AssertOffset(
            LongwatchMuzzleMetadata2D metadata,
            LongwatchMuzzleState2D state,
            int direction,
            int frame,
            Vector2 expected)
        {
            Assert.That(metadata.TryGetMuzzleOffset(state, direction, frame, out Vector2 actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static void AssertPose(
            PlayerLongwatchAimPresenter2D presenter,
            PlayerAnimator2D animator,
            PlayerAim2D aim,
            SpriteRenderer armsRenderer,
            PlayerAnimationState animationState,
            Sprite bodySprite,
            Vector2 aimVector,
            LongwatchMuzzleState2D expectedState,
            int expectedDirection,
            int expectedAngle,
            int expectedFrame,
            bool facingLeft)
        {
            typeof(PlayerAnimator2D).GetField("_currentState",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(animator, (PlayerAnimationState?)animationState);
            Assert.That(animator.CurrentState, Is.EqualTo(animationState));
            Assert.That(aim.ApplyWorldAimVector(aimVector), Is.True);
            presenter.BodySpriteRenderer.sprite = bodySprite;
            armsRenderer.flipX = facingLeft;
            typeof(PlayerLongwatchAimPresenter2D).GetMethod("LateUpdate",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(presenter, null);

            Assert.That(presenter.OwnsRenderer, Is.True, "Longwatch presenter did not acquire the shared renderer.");
            Assert.That(armsRenderer.sprite, Is.Not.Null, "Longwatch presenter did not select an armed sprite.");
            Assert.That(presenter.TryGetCurrentRenderedPose(out LongwatchRenderedPose2D pose), Is.True);
            Assert.That(pose.State, Is.EqualTo(expectedState));
            Assert.That(pose.DirectionIndex, Is.EqualTo(expectedDirection));
            Assert.That(pose.AuthoredAngleDegrees, Is.EqualTo(expectedAngle));
            Assert.That(pose.FrameIndex, Is.EqualTo(expectedFrame));
            Assert.That(pose.FacingLeft, Is.EqualTo(facingLeft));
        }

        private static Vector2 Direction(float angleDegrees, bool facingLeft)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians);
            return new Vector2(facingLeft ? -x : x, Mathf.Sin(radians));
        }
    }
}
