using System;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the deepest non-repeating vertical backdrop to SalvageIntake. The layer follows
    /// camera X exactly and reveals progressively higher source rows as camera altitude increases.
    /// </summary>
    public static class RustlineSalvageIntakeVerticalDepthSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Vertical Depth Backdrop v0 - Managed";
        private const string SpritePath =
            "Assets/Art/Environment/Parallax/VerticalDepth/vertical_depth_backdrop_a.png";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const int SourceWidth = 1080;
        private const int SourceHeight = 2160;
        private const int PixelsPerUnit = 16;
        private const int MinimumImporterSize = 4096;
        private const int MinimumPhaseHeightPixels = 4320;
        private const int SortingOrder = -40;

        // Current Salvage Intake test span: the canonical lower floor surface is y=0 and the highest
        // local walkable structural surface is the overhead deck top at y=19. The 4320-pixel minimum
        // virtual phase height dominates this small room, so only a small fraction of the backdrop is
        // intentionally revealed while climbing the current route.
        private const float PhaseBottomWorldY = 0f;
        private const float PhaseTopWorldY = 19f;

        [MenuItem("Tools/Rustline/Apply Salvage Intake Vertical Depth Backdrop")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Vertical Depth Backdrop applied: screen-locked X, altitude-mapped Y, 4320 px minimum phase span, and 1/16-unit final snapping.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Vertical Depth Backdrop")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the production scene first.");
            ConfigureSpriteImporter();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Vertical Depth Backdrop validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the production scene first.");

            ConfigureSpriteImporter();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(sprite != null, "Vertical depth sprite is unavailable after import: " + SpritePath);
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

            GameObject backdropObject = new GameObject(ManagedRootName);
            backdropObject.transform.SetParent(artRoot, false);
            backdropObject.transform.position = Vector3.zero;
            backdropObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = backdropObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = unlitMaterial;
            renderer.sortingOrder = SortingOrder;

            PixelSnappedVerticalDepthBackdrop2D backdrop =
                backdropObject.AddComponent<PixelSnappedVerticalDepthBackdrop2D>();
            backdrop.Configure(
                PhaseBottomWorldY,
                PhaseTopWorldY,
                SourceHeight,
                MinimumPhaseHeightPixels,
                PixelsPerUnit);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying the Vertical Depth Backdrop.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void ConfigureSpriteImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for Vertical Depth Backdrop: " + SpritePath);

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
            Require(importer != null, "Vertical Depth TextureImporter disappeared after reimport.");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth == SourceWidth && sourceHeight == SourceHeight,
                "Vertical Depth source dimensions changed. Expected " + SourceWidth + "x" + SourceHeight +
                ", got " + sourceWidth + "x" + sourceHeight + ".");
            Require(SourceWidth >= NativePixelViewportMath.MaximumLogicalDimension,
                "Vertical Depth source width must cover the maximum native logical viewport width.");
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Vertical-depth validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Vertical Depth Backdrop v0 managed root is missing.");
            Require(managedRoot.position == Vector3.zero,
                "Vertical Depth managed root must keep its authored world origin at zero outside Play mode.");
            Require(managedRoot.localScale == Vector3.one,
                "Vertical Depth Backdrop must remain at native Transform scale 1.0.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Vertical Depth Backdrop must remain presentation-only and contain no Collider2D.");

            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Material expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            SpriteRenderer renderer = managedRoot.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite == expectedSprite &&
                renderer.sharedMaterial == expectedMaterial && renderer.sortingOrder == SortingOrder,
                "Vertical Depth renderer contract is invalid.");

            PixelSnappedVerticalDepthBackdrop2D backdrop =
                managedRoot.GetComponent<PixelSnappedVerticalDepthBackdrop2D>();
            Require(backdrop != null &&
                Mathf.Approximately(backdrop.PhaseBottomWorldY, PhaseBottomWorldY) &&
                Mathf.Approximately(backdrop.PhaseTopWorldY, PhaseTopWorldY) &&
                backdrop.SourceHeightPixels == SourceHeight &&
                backdrop.MinimumPhaseHeightPixels == MinimumPhaseHeightPixels &&
                backdrop.PixelsPerUnit == PixelsPerUnit &&
                Mathf.Approximately(
                    backdrop.EffectivePhaseHeightWorldUnits,
                    MinimumPhaseHeightPixels / (float)PixelsPerUnit),
                "Vertical Depth altitude-mapping/pixel-snap contract is invalid.");

            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.maxTextureSize >= MinimumImporterSize,
                "Vertical Depth import settings must remain 16 PPU / Point / no mipmaps / uncompressed / >=4096 max texture size.");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth == SourceWidth && sourceHeight == SourceHeight,
                "Vertical Depth source dimensions no longer match the accepted 1080x2160 source.");
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
