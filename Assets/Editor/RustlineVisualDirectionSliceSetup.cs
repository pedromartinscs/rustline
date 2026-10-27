using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>Bounded, noncolliding transfer-bay art. No terrain or gameplay rebuild.</summary>
    public static class RustlineVisualDirectionSliceSetup
    {
        public const string RootName = "Transfer Bay Visual Direction - Phase 1";
        public const string SpritePath = "Assets/Art/Environment/TransferBay/transfer_service_wall.png";
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string MaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [UnityEditor.MenuItem("Tools/Rustline/Visual Direction/Apply Transfer Bay Slice")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyToScene(scene);
            ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save visual slice.");
            Debug.Log("RUSTLINE_VISUAL_PHASE1_OK: transfer-bay steel service wall; existing gameplay and collision untouched.");
        }

        public static void ApplyToScene(Scene scene)
        {
            Transform art = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Single(t => t.name == "Art Dressing - Preserve");
            Transform root = art.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName).transform;
                root.SetParent(art, false);
            }
            // Updating the owned renderer in place keeps scene IDs stable across reapplication.
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = root.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath)
                ?? throw new InvalidOperationException("Missing native service-wall sprite: " + SpritePath);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            renderer.sortingOrder = -24; // In front of far/depth (-30/-40), behind machinery (-16..-10).
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.flipX = renderer.flipY = false;
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;
            root.position = new Vector3(-2f, 8f, 0f); // Bounds [-34,30] x [-16,32], source pixels at 16 PPU.
        }

        [UnityEditor.MenuItem("Tools/Rustline/Visual Direction/Validate Transfer Bay Slice")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            RustlineEnvironmentVisualHardeningSetup.ValidateScene(scene);
            RustlineSalvageIntakeExpansionSetup.ValidateScene(scene);
            RustlineBuildSceneOrder.ValidateOrThrow();
            Debug.Log("RUSTLINE_VISUAL_PHASE1_VALIDATED: native art, original material distinctions, expansion and scene order.");
        }

        public static void ValidateScene(Scene scene)
        {
            Transform root = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(t => t.name == RootName);
            if (root == null || root.parent.name != "Art Dressing - Preserve")
                throw new InvalidOperationException("Missing preserved transfer-bay slice.");
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite != AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) ||
                renderer.sortingOrder != -24 || renderer.color != Color.white ||
                renderer.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ||
                root.position != new Vector3(-2f, 8f, 0f) || root.lossyScale != Vector3.one ||
                root.rotation != Quaternion.identity || renderer.drawMode != SpriteDrawMode.Simple ||
                renderer.flipX || renderer.flipY || !renderer.enabled)
                throw new InvalidOperationException("Transfer-bay native-scale/sorting/material contract changed.");
            if (root.childCount != 0 || root.GetComponents<Component>().Length != 2)
            {
                // Transform + SpriteRenderer only. Never attach colliders, lights, or runtime work here.
                throw new InvalidOperationException("Transfer-bay dressing must contain only a static sprite.");
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            if (importer.spritePixelsPerUnit != 16 || importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                renderer.sprite.texture.width != 1024 || renderer.sprite.texture.height != 768)
                throw new InvalidOperationException("Transfer-bay art was rescaled, filtered, or compressed.");
        }
    }
}
