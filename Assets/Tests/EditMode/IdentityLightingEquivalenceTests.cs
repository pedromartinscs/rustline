using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class IdentityLightingEquivalenceTests
    {
        private const string ArtShowcasePath = "Assets/Scenes/ArtShowcase.unity";
        private const string MovementLabPath = "Assets/Scenes/MovementLab.unity";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string JumpDustPrefabPath =
            "Assets/Prefabs/Effects/Movement/PlayerJumpDust.prefab";
        private const string LitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string UnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string LitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";
        private const int CaptureWidth = 1024;
        private const int CaptureHeight = 768;

        [Test]
        public void Renderer2D_DefaultsToUnlitAndRetainsBothUrpMaterials()
        {
            Object rendererData = AssetDatabase.LoadMainAssetAtPath(Renderer2DPath);
            Assert.That(rendererData, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(rendererData);
            Assert.That(serialized.FindProperty("m_DefaultMaterialType").intValue, Is.EqualTo(1));
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string rendererYaml = File.ReadAllText(Path.Combine(projectRoot, Renderer2DPath));
            StringAssert.Contains(
                "m_DefaultLitMaterial: {fileID: 2100000, guid: " +
                LitMaterialGuid,
                rendererYaml);
            StringAssert.Contains(
                "m_DefaultUnlitMaterial: {fileID: 2100000, guid: " +
                UnlitMaterialGuid,
                rendererYaml);
        }

        [TestCase(MovementLabPath)]
        [TestCase(ArtShowcasePath)]
        public void CurrentScene_UsesUnlitForOrdinaryRenderersAndHasNoIdentityGlobalLight(
            string scenePath)
        {
            SceneSetup[] originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
                List<Renderer> ordinaryRenderers = FindOrdinaryRenderers(scene);
                Assert.That(ordinaryRenderers, Is.Not.Empty);
                foreach (Renderer renderer in ordinaryRenderers)
                {
                    Assert.That(renderer.sharedMaterial, Is.EqualTo(unlitMaterial), renderer.name);
                }

                Assert.That(FindSceneGameObject(scene, "Global Light 2D"), Is.Null);
            }
            finally
            {
                RestoreSceneSetup(originalSceneSetup);
            }
        }

        [Test]
        public void PlayerAndJumpDustPrefabs_UseUnlitForOrdinaryRenderers()
        {
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject jumpDust = AssetDatabase.LoadAssetAtPath<GameObject>(JumpDustPrefabPath);
            Assert.That(player, Is.Not.Null);
            Assert.That(jumpDust, Is.Not.Null);
            foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(renderer.sharedMaterial, Is.EqualTo(unlitMaterial), renderer.name);
            }

            Assert.That(
                jumpDust.GetComponent<SpriteRenderer>().sharedMaterial,
                Is.EqualTo(unlitMaterial));
        }

        [Test]
        public void ArtShowcase_IdentityGlobalLightAndUnlitRenderExactlyEqual()
        {
            SceneSetup[] originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            RenderTexture target = null;
            Texture2D readback = null;
            GameObject temporaryLightObject = null;

            try
            {
                Scene scene = EditorSceneManager.OpenScene(ArtShowcasePath, OpenSceneMode.Single);
                Camera camera = FindSceneComponent<Camera>(scene);
                Assert.That(camera, Is.Not.Null);

                Material litMaterial = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
                Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
                Assert.That(litMaterial, Is.Not.Null);
                Assert.That(unlitMaterial, Is.Not.Null);

                List<Renderer> ordinaryRenderers = FindOrdinaryRenderers(scene);
                Assert.That(ordinaryRenderers, Is.Not.Empty);
                Material[] originalMaterials = new Material[ordinaryRenderers.Count];
                for (int index = 0; index < ordinaryRenderers.Count; index++)
                {
                    originalMaterials[index] = ordinaryRenderers[index].sharedMaterial;
                }

                temporaryLightObject = new GameObject("Identity Global Light 2D - Comparison");
                SceneManager.MoveGameObjectToScene(temporaryLightObject, scene);
                Light2D identityLight = temporaryLightObject.AddComponent<Light2D>();
                RenderTexture originalTarget = camera.targetTexture;

                target = new RenderTexture(
                    CaptureWidth,
                    CaptureHeight,
                    16,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                target.Create();
                readback = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGBA32,
                    false,
                    false);
                camera.targetTexture = target;

                try
                {
                    SetMaterials(ordinaryRenderers, litMaterial);
                    identityLight.lightType = Light2D.LightType.Global;
                    identityLight.color = Color.white;
                    identityLight.intensity = 1f;
                    identityLight.enabled = true;
                    Color32[] litPixels = RenderAndRead(camera, target, readback);

                    SetMaterials(ordinaryRenderers, unlitMaterial);
                    identityLight.enabled = false;
                    Color32[] unlitPixels = RenderAndRead(camera, target, readback);

                    AssertExactPixels(litPixels, unlitPixels);
                }
                finally
                {
                    for (int index = 0; index < ordinaryRenderers.Count; index++)
                    {
                        ordinaryRenderers[index].sharedMaterial = originalMaterials[index];
                    }

                    camera.targetTexture = originalTarget;
                }
            }
            finally
            {
                if (temporaryLightObject != null)
                {
                    Object.DestroyImmediate(temporaryLightObject);
                }

                if (readback != null)
                {
                    Object.DestroyImmediate(readback);
                }

                if (target != null)
                {
                    target.Release();
                    Object.DestroyImmediate(target);
                }

                RestoreSceneSetup(originalSceneSetup);
            }
        }

        private static void RestoreSceneSetup(SceneSetup[] sceneSetup)
        {
            bool hasLoadedScene = false;
            for (int index = 0; index < sceneSetup.Length; index++)
            {
                hasLoadedScene |= sceneSetup[index].isLoaded;
            }

            if (hasLoadedScene)
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static List<Renderer> FindOrdinaryRenderers(Scene scene)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                renderers.AddRange(root.GetComponentsInChildren<SpriteRenderer>(true));
                renderers.AddRange(root.GetComponentsInChildren<TilemapRenderer>(true));
            }

            return renderers;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject FindSceneGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        private static void SetMaterials(IReadOnlyList<Renderer> renderers, Material material)
        {
            for (int index = 0; index < renderers.Count; index++)
            {
                renderers[index].sharedMaterial = material;
            }
        }

        private static Color32[] RenderAndRead(
            Camera camera,
            RenderTexture target,
            Texture2D readback)
        {
            camera.Render();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(
                    new Rect(0f, 0f, target.width, target.height),
                    0,
                    0,
                    false);
                readback.Apply(false, false);
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static void AssertExactPixels(Color32[] expected, Color32[] actual)
        {
            Assert.That(actual, Has.Length.EqualTo(expected.Length));
            int differingPixels = 0;
            int maximumChannelDelta = 0;
            List<string> examples = new List<string>(8);

            for (int index = 0; index < expected.Length; index++)
            {
                Color32 lit = expected[index];
                Color32 unlit = actual[index];
                int redDelta = Mathf.Abs(lit.r - unlit.r);
                int greenDelta = Mathf.Abs(lit.g - unlit.g);
                int blueDelta = Mathf.Abs(lit.b - unlit.b);
                int alphaDelta = Mathf.Abs(lit.a - unlit.a);
                int pixelMaximum = Mathf.Max(
                    Mathf.Max(redDelta, greenDelta),
                    Mathf.Max(blueDelta, alphaDelta));
                if (pixelMaximum == 0)
                {
                    continue;
                }

                differingPixels++;
                maximumChannelDelta = Mathf.Max(maximumChannelDelta, pixelMaximum);
                if (examples.Count < examples.Capacity)
                {
                    int x = index % CaptureWidth;
                    int y = index / CaptureWidth;
                    examples.Add($"({x},{y}) Lit={lit} Unlit={unlit}");
                }
            }

            TestContext.Out.WriteLine(
                $"Identity lighting comparison: pixels={expected.Length}, " +
                $"different={differingPixels}, maxChannelDelta={maximumChannelDelta}.");
            Assert.That(
                differingPixels,
                Is.EqualTo(0),
                $"Lit/identity-light and Unlit/no-light output differed. " +
                $"Maximum channel delta: {maximumChannelDelta}. " +
                string.Join("; ", examples));
        }
    }
}
