using System;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the first far-background parallax layer to SalvageIntake. The layer is presentation
    /// only, lives under Art Dressing - Preserve, and resolves the runtime Main Camera dynamically so
    /// rebuilding the managed player/camera rig cannot leave a stale serialized camera reference.
    /// </summary>
    public static class RustlineSalvageIntakeParallaxSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Far Parallax v0 - Managed";
        private const string SpritePath =
            "Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const int SourceWidth = 1296;
        private const int SourceHeight = 410;
        private const int PixelsPerUnit = 16;
        private const int SortingOrder = -30;
        private const float HorizontalFollow = 0.94f;
        private const float VerticalFollow = 0.96f;

        // 1296x410 at 16 PPU is 81x25.625 u. This anchor covers the west-entry camera while leaving
        // deliberate Deep Space negative space, and the near-camera follow keeps the same panorama
        // useful as the player moves through the east shaft/continuation.
        private static readonly Vector3 AnchorPosition = new Vector3(-3.5f, 4.8125f, 0f);

        [MenuItem("Tools/Rustline/Apply Salvage Intake Far Parallax")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Far industrial parallax was applied at 0.94x / 0.96y camera follow with 1/16-unit pixel snapping.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Far Parallax")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the production scene first.");
            ConfigureSpriteImporter();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Far industrial parallax validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the production scene first.");

            ConfigureSpriteImporter();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(sprite != null, "Far industrial parallax sprite is unavailable after import: " + SpritePath);
            Require(unlitMaterial != null, "Sprite-Unlit-Default material is unavailable.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing. Rebuild the production scene first.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Require(artRoot != null,
                "Art Dressing - Preserve is missing. Rebuild the SalvageIntake production scene first.");

            Transform previous = artRoot.Find(ManagedRootName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject parallaxObject = new GameObject(ManagedRootName);
            parallaxObject.transform.SetParent(artRoot, false);
            parallaxObject.transform.position = AnchorPosition;
            parallaxObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = parallaxObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = unlitMaterial;
            renderer.sortingOrder = SortingOrder;

            PixelSnappedParallax2D parallax = parallaxObject.AddComponent<PixelSnappedParallax2D>();
            parallax.Configure(HorizontalFollow, VerticalFollow, PixelsPerUnit);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying far parallax.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void ConfigureSpriteImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for far parallax: " + SpritePath);

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                importer.spritePixelsPerUnit = PixelsPerUnit;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Require(importer != null, "Far parallax TextureImporter disappeared after reimport.");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth == SourceWidth && sourceHeight == SourceHeight,
                "Far parallax source dimensions changed. Expected " + SourceWidth + "x" + SourceHeight +
                ", got " + sourceWidth + "x" + sourceHeight + ".");
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Far-parallax validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Far Parallax v0 managed root is missing.");
            Require(managedRoot.position == AnchorPosition,
                "Far parallax anchor moved away from its native-pixel composition position.");
            Require(managedRoot.localScale == Vector3.one,
                "Far parallax must remain at native Transform scale 1.0.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Far parallax must remain presentation-only and contain no Collider2D.");

            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            SpriteRenderer renderer = managedRoot.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite == expectedSprite &&
                renderer.sharedMaterial == expectedMaterial && renderer.sortingOrder == SortingOrder,
                "Far parallax renderer contract is invalid.");

            PixelSnappedParallax2D parallax = managedRoot.GetComponent<PixelSnappedParallax2D>();
            Require(parallax != null &&
                Mathf.Approximately(parallax.HorizontalFollow, HorizontalFollow) &&
                Mathf.Approximately(parallax.VerticalFollow, VerticalFollow) &&
                parallax.PixelsPerUnit == PixelsPerUnit,
                "Far parallax camera-follow/pixel-snap contract is invalid.");

            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed,
                "Far parallax import settings must remain 16 PPU / Point / no mipmaps / uncompressed.");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth == SourceWidth && sourceHeight == SourceHeight,
                "Far parallax source dimensions no longer match the accepted 1296x410 source.");
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform transform in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }
            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
