using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class LayeredPlayerArtTests
    {
        private const string BodyRoot = "Assets/Art/Characters/Player/Sprites/Body";
        private const string ArmsRoot = "Assets/Art/Characters/Player/Sprites/Arms/Unarmed";
        private const string BodyAnimationRoot = "Assets/Art/Characters/Player/Animations/Body";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

        [TestCase("idle", 2)]
        [TestCase("run", 6)]
        [TestCase("jump", 3)]
        [TestCase("fall", 1)]
        [TestCase("land", 2)]
        public void LayeredSheets_MatchCanonicalImportAndCellContract(string state, int expectedFrames)
        {
            string bodyPath = BodyRoot + "/player_salvager_body_" + state + ".png";
            string armsPath = ArmsRoot + "/player_salvager_arms_" + state + ".png";
            Texture2D bodyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bodyPath);
            Texture2D armsTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(armsPath);

            Assert.That(bodyTexture, Is.Not.Null, "Missing Body production sheet.");
            Assert.That(armsTexture, Is.Not.Null, "Missing Unarmed Arms production sheet.");
            Assert.That(bodyTexture.width, Is.EqualTo(expectedFrames * 48));
            Assert.That(bodyTexture.height, Is.EqualTo(64));
            Assert.That(armsTexture.width, Is.EqualTo(bodyTexture.width));
            Assert.That(armsTexture.height, Is.EqualTo(bodyTexture.height));

            AssertImporter(bodyPath);
            AssertImporter(armsPath);
            AssertSprites(bodyPath, "player_salvager_body_" + state, expectedFrames);
            AssertSprites(armsPath, "player_salvager_arms_" + state, expectedFrames);
            AssertSourcePixels(bodyPath);
            AssertSourcePixels(armsPath);
        }

        [Test]
        public void PlayerPrefab_HasCompleteOneToOneBodyToArmsMapping()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            PlayerUnarmedArmsPresenter2D presenter = prefab.GetComponent<PlayerUnarmedArmsPresenter2D>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.MappingCount, Is.EqualTo(14));

            HashSet<Sprite> bodySprites = new HashSet<Sprite>();
            HashSet<Sprite> armsSprites = new HashSet<Sprite>();
            foreach ((string state, int count) in StateSpecs())
            {
                List<Sprite> expectedBodies = LoadSprites(
                    BodyRoot + "/player_salvager_body_" + state + ".png");
                List<Sprite> expectedArms = LoadSprites(
                    ArmsRoot + "/player_salvager_arms_" + state + ".png");
                Assert.That(expectedBodies, Has.Count.EqualTo(count));
                Assert.That(expectedArms, Has.Count.EqualTo(count));
                for (int index = 0; index < count; index++)
                {
                    Assert.That(bodySprites.Add(expectedBodies[index]), Is.True, "Duplicate Body mapping input.");
                    Assert.That(presenter.TryGetArmsSprite(expectedBodies[index], out Sprite mappedArms), Is.True,
                        "Missing mapping for " + expectedBodies[index].name);
                    Assert.That(mappedArms, Is.SameAs(expectedArms[index]));
                    Assert.That(armsSprites.Add(mappedArms), Is.True, "Duplicate Arms mapping output.");
                }
            }

            Assert.That(bodySprites, Has.Count.EqualTo(14));
            Assert.That(armsSprites, Has.Count.EqualTo(14));
        }

        [Test]
        public void JumpClip_PlaysTakeoffOnceThenHoldsFinalFrame()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                BodyAnimationRoot + "/Player_Body_Jump.anim");
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.frameRate, Is.EqualTo(20f));
            Assert.That(clip.wrapMode, Is.EqualTo(WrapMode.ClampForever));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.False);

            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            Assert.That(bindings, Has.Length.EqualTo(1));
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
            Assert.That(keyframes, Has.Length.EqualTo(3));

            List<Sprite> expectedSprites = LoadSprites(BodyRoot + "/player_salvager_body_jump.png");
            float[] expectedTimes = { 0f, 0.05f, 0.1f };
            for (int index = 0; index < keyframes.Length; index++)
            {
                Assert.That(keyframes[index].time, Is.EqualTo(expectedTimes[index]).Within(0.0001f));
                Assert.That(keyframes[index].value, Is.SameAs(expectedSprites[index]));
            }
        }

        private static void AssertImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.crunchedCompression, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
            Assert.That(settings.spriteGenerateFallbackPhysicsShape, Is.False);
        }

        private static void AssertSprites(string path, string expectedPrefix, int expectedFrames)
        {
            List<Sprite> sprites = LoadSprites(path);
            Assert.That(sprites, Has.Count.EqualTo(expectedFrames));
            for (int index = 0; index < sprites.Count; index++)
            {
                Sprite sprite = sprites[index];
                Assert.That(sprite.name, Is.EqualTo(expectedPrefix + "_" + index));
                Assert.That(sprite.rect, Is.EqualTo(new Rect(index * 48, 0, 48, 64)));
                Assert.That(Vector2.Distance(sprite.pivot, new Vector2(24f, 0f)), Is.LessThan(0.001f));
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16f));
            }
        }

        private static void AssertSourcePixels(string path)
        {
            string absolutePath = System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath).FullName,
                path);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Assert.That(source.LoadImage(System.IO.File.ReadAllBytes(absolutePath), false), Is.True);
                foreach (Color32 pixel in source.GetPixels32())
                {
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True, path + " contains partial alpha.");
                    if (pixel.a == 255)
                    {
                        Assert.That(RustlinePalette.IsCanonical(pixel), Is.True,
                            path + " contains an opaque pixel outside Canonical 28.");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static List<Sprite> LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)))
                .ToList();
        }

        private static IEnumerable<(string state, int count)> StateSpecs()
        {
            yield return ("idle", 2);
            yield return ("run", 6);
            yield return ("jump", 3);
            yield return ("fall", 1);
            yield return ("land", 2);
        }
    }
}
