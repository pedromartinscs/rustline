using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class IdentityLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator CurrentScenes_RunWithUnlitOrdinaryRenderersAndNoIdentityGlobalLight()
        {
            string[] sceneNames = { "MovementLab", "ArtShowcase" };
            foreach (string sceneName in sceneNames)
            {
                SceneManager.LoadScene(sceneName);
                yield return null;
                yield return null;

                Scene scene = SceneManager.GetActiveScene();
                List<Renderer> ordinaryRenderers = FindOrdinaryRenderers(scene);
                Assert.That(ordinaryRenderers, Is.Not.Empty, sceneName);
                foreach (Renderer renderer in ordinaryRenderers)
                {
                    Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name);
                    Assert.That(
                        renderer.sharedMaterial.name,
                        Is.EqualTo("Sprite-Unlit-Default"),
                        sceneName + ": " + renderer.name);
                }

                Assert.That(FindSceneGameObject(scene, "Global Light 2D"), Is.Null, sceneName);
            }
        }

        private static List<Renderer> FindOrdinaryRenderers(Scene scene)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                AddOrdinaryRenderers(
                    renderers,
                    root.GetComponentsInChildren<SpriteRenderer>(true));
                AddOrdinaryRenderers(
                    renderers,
                    root.GetComponentsInChildren<TilemapRenderer>(true));
            }

            return renderers;
        }

        private static void AddOrdinaryRenderers<T>(
            List<Renderer> destination,
            T[] candidates)
            where T : Renderer
        {
            for (int index = 0; index < candidates.Length; index++)
            {
                Renderer renderer = candidates[index];

                // RustlineHUD is a deliberately separate native-pixel presentation path.
                // Its card uses the palette-fade shader rather than the ordinary world
                // Sprite-Unlit-Default material, and is validated by selector-specific tests.
                if (renderer.gameObject.layer == WeaponCarouselHud2D.RustlineHudLayerIndex)
                {
                    continue;
                }

                destination.Add(renderer);
            }
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
    }
}
