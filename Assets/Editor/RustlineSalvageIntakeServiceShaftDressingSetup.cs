using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the first production-art pass to Salvage Intake's canonical east service shaft.
    /// The authored shaft sprites replace only the generic visual Tilemap cells; hidden collision
    /// remains untouched and authoritative so Wall Brace / Wall Kick traversal cannot drift.
    /// </summary>
    public static class RustlineSalvageIntakeServiceShaftDressingSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Service Shaft Wall Skin v0 - Managed";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string LeftWallPath =
            "Assets/Art/Environment/Architecture/ServiceShaft/shaft_wall_a.png";
        private const string RightWallPath =
            "Assets/Art/Environment/Architecture/ServiceShaft/shaft_wall_b.png";

        private const int PixelsPerUnit = 16;
        private const int ExpectedHeightPixels = 240;
        private const int MinimumWidthPixels = 32;
        private const int MaximumWidthPixels = 64;
        private const int SortingOrder = 2;

        // Canonical collision planes. The left skin grows outward from x=28 and starts at y=4;
        // the right skin grows outward from x=32 and starts at y=0. Neither may intrude into the
        // four-unit / 64-pixel playable shaft interior x=28..32.
        private const float LeftInnerFaceX = 28f;
        private const float LeftBottomY = 4f;
        private const float RightInnerFaceX = 32f;
        private const float RightBottomY = 0f;

        private const bool LeftFlipX = false;
        private const bool RightFlipX = false;

        [MenuItem("Tools/Rustline/Apply Salvage Intake Service Shaft Dressing")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Service-shaft wall dressing applied at native scale. Hidden collision and the 64 px playable interior were preserved.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Service Shaft Dressing")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the production scene first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Service-shaft wall dressing validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the production scene first.");

            Sprite leftSprite = RequireProductionSprite(LeftWallPath);
            Sprite rightSprite = RequireProductionSprite(RightWallPath);
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "Sprite-Unlit-Default material is unavailable.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing. Rebuild the production scene first.");

            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Require(artRoot != null,
                "Art Dressing - Preserve is missing. Rebuild the SalvageIntake production scene first.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing. Rebuild the production scene first.");

            foreach (Vector3Int cell in EnumerateLeftWallCells())
            {
                Require(collision.HasTile(cell), "Expected hidden left shaft-wall collision is missing at " + cell + ".");
                structure.SetTile(cell, null);
            }
            foreach (Vector3Int cell in EnumerateRightWallCells())
            {
                Require(collision.HasTile(cell), "Expected hidden right shaft-wall collision is missing at " + cell + ".");
                structure.SetTile(cell, null);
            }
            structure.RefreshAllTiles();
            EditorUtility.SetDirty(structure);

            Transform previous = artRoot.Find(ManagedRootName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject managedRoot = new GameObject(ManagedRootName);
            managedRoot.transform.SetParent(artRoot, false);
            managedRoot.transform.localPosition = Vector3.zero;
            managedRoot.transform.localScale = Vector3.one;

            CreateWallSprite(
                managedRoot.transform,
                "Service Shaft Wall - Left A",
                leftSprite,
                unlitMaterial,
                alignRightEdgeWorldX: LeftInnerFaceX,
                bottomWorldY: LeftBottomY,
                flipX: LeftFlipX);

            CreateWallSprite(
                managedRoot.transform,
                "Service Shaft Wall - Right B",
                rightSprite,
                unlitMaterial,
                alignLeftEdgeWorldX: RightInnerFaceX,
                bottomWorldY: RightBottomY,
                flipX: RightFlipX);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying service-shaft wall dressing.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void CreateWallSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Material material,
            float? alignLeftEdgeWorldX = null,
            float? alignRightEdgeWorldX = null,
            float bottomWorldY = 0f,
            bool flipX = false)
        {
            Require(alignLeftEdgeWorldX.HasValue ^ alignRightEdgeWorldX.HasValue,
                name + " must align exactly one horizontal source edge.");

            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
            renderer.flipX = flipX;

            float leftOffset = -sprite.pivot.x / PixelsPerUnit;
            float rightOffset = (sprite.rect.width - sprite.pivot.x) / PixelsPerUnit;
            if (flipX)
            {
                float flippedLeft = -rightOffset;
                float flippedRight = -leftOffset;
                leftOffset = flippedLeft;
                rightOffset = flippedRight;
            }

            float x = alignLeftEdgeWorldX.HasValue
                ? alignLeftEdgeWorldX.Value - leftOffset
                : alignRightEdgeWorldX.Value - rightOffset;
            float y = bottomWorldY + sprite.pivot.y / PixelsPerUnit;
            spriteObject.transform.position = new Vector3(x, y, 0f);
        }

        private static Sprite RequireProductionSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
            Require(sprite != null, "Missing production sprite: " + assetPath);
            Require(Mathf.Approximately(sprite.pixelsPerUnit, PixelsPerUnit),
                assetPath + " must import at 16 PPU.");

            int rectWidth = Mathf.RoundToInt(sprite.rect.width);
            int rectHeight = Mathf.RoundToInt(sprite.rect.height);
            Require(rectHeight == ExpectedHeightPixels,
                assetPath + " must remain exactly " + ExpectedHeightPixels + " px tall; got " + rectHeight + ".");
            Require(rectWidth >= MinimumWidthPixels && rectWidth <= MaximumWidthPixels,
                assetPath + " width must remain between " + MinimumWidthPixels + " and " + MaximumWidthPixels +
                " px so the skin can cover the 32 px collision wall without excessive overhang; got " + rectWidth + ".");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for production sprite: " + assetPath);
            Require(importer.filterMode == FilterMode.Point,
                assetPath + " must use Point filtering.");
            Require(!importer.mipmapEnabled,
                assetPath + " must keep mipmaps disabled.");
            Require(importer.alphaIsTransparency,
                assetPath + " must preserve transparent negative space.");

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceHeight == ExpectedHeightPixels &&
                sourceWidth >= MinimumWidthPixels && sourceWidth <= MaximumWidthPixels,
                assetPath + " source dimensions are outside the accepted shaft-wall envelope: " +
                sourceWidth + "x" + sourceHeight + ".");

            return sprite;
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Service-shaft dressing validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Service Shaft Wall Skin v0 managed root is missing.");
            Require(managedRoot.localScale == Vector3.one,
                "Service Shaft Wall Skin v0 must remain at native Transform scale 1.0.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Service-shaft dressing must remain presentation-only and contain no Collider2D.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing.");

            foreach (Vector3Int cell in EnumerateLeftWallCells())
            {
                Require(!structure.HasTile(cell),
                    "Generic visual tile remains beneath the left production shaft wall at " + cell + ".");
                Require(collision.HasTile(cell),
                    "Left production shaft wall must not remove hidden collision at " + cell + ".");
            }
            foreach (Vector3Int cell in EnumerateRightWallCells())
            {
                Require(!structure.HasTile(cell),
                    "Generic visual tile remains beneath the right production shaft wall at " + cell + ".");
                Require(collision.HasTile(cell),
                    "Right production shaft wall must not remove hidden collision at " + cell + ".");
            }

            ValidateWallObject(
                managedRoot,
                "Service Shaft Wall - Left A",
                RequireProductionSprite(LeftWallPath),
                LeftInnerFaceX,
                LeftBottomY,
                alignLeftEdge: false,
                flipX: LeftFlipX);
            ValidateWallObject(
                managedRoot,
                "Service Shaft Wall - Right B",
                RequireProductionSprite(RightWallPath),
                RightInnerFaceX,
                RightBottomY,
                alignLeftEdge: true,
                flipX: RightFlipX);

            Require(managedRoot.GetComponentsInChildren<SpriteRenderer>(true).Length == 2,
                "Service Shaft Wall Skin v0 must contain exactly two wall SpriteRenderers.");
        }

        private static void ValidateWallObject(
            Transform managedRoot,
            string name,
            Sprite expectedSprite,
            float expectedInnerFaceX,
            float expectedBottomY,
            bool alignLeftEdge,
            bool flipX)
        {
            Transform child = managedRoot.Find(name);
            Require(child != null, "Missing shaft-wall dressing object: " + name);
            Require(child.localScale == Vector3.one,
                name + " must remain at native scale 1.0.");

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite == expectedSprite &&
                renderer.sortingOrder == SortingOrder && renderer.flipX == flipX,
                name + " renderer contract is invalid.");

            float leftOffset = -expectedSprite.pivot.x / PixelsPerUnit;
            float rightOffset = (expectedSprite.rect.width - expectedSprite.pivot.x) / PixelsPerUnit;
            if (flipX)
            {
                float flippedLeft = -rightOffset;
                float flippedRight = -leftOffset;
                leftOffset = flippedLeft;
                rightOffset = flippedRight;
            }

            float actualInnerFaceX = child.position.x + (alignLeftEdge ? leftOffset : rightOffset);
            float actualBottomY = child.position.y - expectedSprite.pivot.y / PixelsPerUnit;
            Require(Mathf.Approximately(actualInnerFaceX, expectedInnerFaceX),
                name + " no longer aligns to the canonical playable shaft face at x=" + expectedInnerFaceX + ".");
            Require(Mathf.Approximately(actualBottomY, expectedBottomY),
                name + " no longer aligns to the canonical shaft-wall bottom at y=" + expectedBottomY + ".");
        }

        private static Vector3Int[] EnumerateLeftWallCells()
        {
            Vector3Int[] cells = new Vector3Int[30];
            int index = 0;
            for (int y = 4; y < 19; y++)
            {
                cells[index++] = new Vector3Int(26, y, 0);
                cells[index++] = new Vector3Int(27, y, 0);
            }
            return cells;
        }

        private static Vector3Int[] EnumerateRightWallCells()
        {
            Vector3Int[] cells = new Vector3Int[30];
            int index = 0;
            for (int y = 0; y < 15; y++)
            {
                cells[index++] = new Vector3Int(32, y, 0);
                cells[index++] = new Vector3Int(33, y, 0);
            }
            return cells;
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

        private static Tilemap FindTilemap(Scene scene, string name)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in sceneRoot.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.name == name)
                    {
                        return tilemap;
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
