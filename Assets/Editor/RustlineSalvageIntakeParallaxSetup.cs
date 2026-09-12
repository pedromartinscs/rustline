using System;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the production horizontal far-parallax layer to SalvageIntake. The layer is
    /// presentation-only, lives under Art Dressing - Preserve, repeats seamlessly on X, and is
    /// screen-locked on Y so vertical camera movement never makes the backdrop float in the frame.
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

        private const int SourceWidth = 2160;
        private const int SourceHeight = 1080;
        private const int PixelsPerUnit = 16;
        private const int MinimumImporterSize = 4096;
        private const int SortingOrder = -30;
        private const float HorizontalFollow = 0.94f;
        private const int HorizontalPhaseSourcePixels = 0;
        private const int VerticalScreenOffsetSourcePixels = 0;

        private static readonly string[] SegmentNames =
        {
            "Segment - Left",
            "Segment - Center",
            "Segment - Right"
        };

        private static readonly int[] SegmentOffsets = { -1, 0, 1 };

        private static float TileWidthWorldUnits => SourceWidth / (float)PixelsPerUnit;

        [MenuItem("Tools/Rustline/Apply Salvage Intake Far Parallax")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Horizontal far parallax was applied as a seamless 2160x1080 loop: 0.94x follow on X, screen-locked Y, and 1/16-unit final snapping.",
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
                "Horizontal far-parallax validation passed.",
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
            parallaxObject.transform.position = Vector3.zero;
            parallaxObject.transform.localScale = Vector3.one;

            for (int i = 0; i < SegmentNames.Length; i++)
            {
                CreateSegment(
                    parallaxObject.transform,
                    SegmentNames[i],
                    SegmentOffsets[i],
                    sprite,
                    unlitMaterial);
            }

            PixelSnappedParallax2D parallax = parallaxObject.AddComponent<PixelSnappedParallax2D>();
            parallax.ConfigureHorizontalLoop(
                HorizontalFollow,
                SourceWidth,
                PixelsPerUnit,
                HorizontalPhaseSourcePixels,
                VerticalScreenOffsetSourcePixels);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying horizontal far parallax.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void CreateSegment(
            Transform parent,
            string name,
            int horizontalTileOffset,
            Sprite sprite,
            Material material)
        {
            GameObject segment = new GameObject(name);
            segment.transform.SetParent(parent, false);
            segment.transform.localPosition = new Vector3(horizontalTileOffset * TileWidthWorldUnits, 0f, 0f);
            segment.transform.localScale = Vector3.one;

            SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
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
            if (importer.maxTextureSize < MinimumImporterSize)
            {
                importer.maxTextureSize = MinimumImporterSize;
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
            Require(managedRoot.position == Vector3.zero,
                "Horizontal far-parallax managed root must keep its authored world origin at zero outside Play mode.");
            Require(managedRoot.localScale == Vector3.one,
                "Far parallax must remain at native Transform scale 1.0.");
            Require(managedRoot.GetComponent<SpriteRenderer>() == null,
                "Horizontal far-parallax root is a controller; rendering belongs to its three child segments.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Far parallax must remain presentation-only and contain no Collider2D.");
            Require(managedRoot.childCount == SegmentNames.Length,
                "Horizontal far parallax must contain exactly three adjacent managed segments.");

            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(expectedSprite != null && expectedMaterial != null,
                "Horizontal far-parallax sprite/material assets are unavailable during validation.");

            for (int i = 0; i < SegmentNames.Length; i++)
            {
                Transform segment = managedRoot.Find(SegmentNames[i]);
                Require(segment != null, "Missing horizontal far-parallax segment: " + SegmentNames[i]);
                Vector3 expectedLocalPosition = new Vector3(SegmentOffsets[i] * TileWidthWorldUnits, 0f, 0f);
                Require(segment.localPosition == expectedLocalPosition && segment.localScale == Vector3.one,
                    SegmentNames[i] + " is not aligned at one exact 2160-pixel tile interval.");

                SpriteRenderer renderer = segment.GetComponent<SpriteRenderer>();
                Require(renderer != null && renderer.sprite == expectedSprite &&
                    renderer.sharedMaterial == expectedMaterial && renderer.sortingOrder == SortingOrder,
                    SegmentNames[i] + " renderer contract is invalid.");
            }

            PixelSnappedParallax2D parallax = managedRoot.GetComponent<PixelSnappedParallax2D>();
            Require(parallax != null &&
                Mathf.Approximately(parallax.HorizontalFollow, HorizontalFollow) &&
                parallax.TileWidthSourcePixels == SourceWidth &&
                parallax.HorizontalPhaseSourcePixels == HorizontalPhaseSourcePixels &&
                parallax.VerticalScreenOffsetSourcePixels == VerticalScreenOffsetSourcePixels &&
                parallax.PixelsPerUnit == PixelsPerUnit,
                "Horizontal far-parallax loop/pixel-snap contract is invalid.");

            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.maxTextureSize >= MinimumImporterSize,
                "Far parallax import settings must remain 16 PPU / Point / no mipmaps / uncompressed / >=4096 max texture size.");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth == SourceWidth && sourceHeight == SourceHeight,
                "Far parallax source dimensions no longer match the accepted 2160x1080 source.");
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
