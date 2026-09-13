using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Replaces the two lowest visual RuleTile rows of the Salvage Intake safety floor with
    /// production Floor art while preserving the hidden collision Tilemap. This creates a visual
    /// non-RuleTile envelope that can remain indestructible when structural breaching is introduced.
    /// </summary>
    public static class RustlineSalvageIntakeSafetyFloorArmorSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Safety Floor Armor v0 - Managed";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string FloorLeftPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_left.png";
        private const string FloorMidPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_mid_a.png";
        private const string FloorRightPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_right.png";

        private const int SourceTextureWidth = 48;
        private const int SourceTextureHeight = 32;
        private const int LeftSpriteWidth = 48;
        private const int MidSpriteWidth = 48;
        private const int RightSpriteWidth = 47;
        private const int SpriteHeight = 32;
        private const int PixelsPerUnit = 16;
        private const int SortingOrder = 2;

        private const int FloorLeftCell = -28;
        private const int FloorRightExclusiveCell = 34;
        private const int ArmorBottomCell = -4;
        private const int ArmorTopExclusiveCell = -2;

        // All Floor sprites use a bottom-left pivot. The right cap is intentionally sliced to 47 px
        // even though its source PNG is 48 px wide, so the tiled middle absorbs that one-pixel
        // difference. The complete composition remains exactly 62 u / 992 px wide and 2 u / 32 px
        // tall: x=-28..34, y=-4..-2, with no fractional-pixel placement or Transform scaling.
        private static readonly Vector2 LeftPosition = new Vector2(-28f, -4f);
        private static readonly Vector2 MidPosition = new Vector2(-25f, -4f);
        private static readonly Vector2 MidSize = new Vector2(56.0625f, 2f);
        private static readonly Vector2 RightPosition = new Vector2(31.0625f, -4f);

        [MenuItem("Tools/Rustline/Apply Salvage Intake Safety Floor Armor")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "The two lowest safety-floor rows are now covered by non-RuleTile production floor armor.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Safety Floor Armor")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Safety-floor armor validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(material != null, "Sprite-Unlit-Default material is unavailable.");

            Sprite leftSprite = RequireProductionSprite(FloorLeftPath, LeftSpriteWidth);
            Sprite midSprite = RequireProductionSprite(FloorMidPath, MidSpriteWidth);
            Sprite rightSprite = RequireProductionSprite(FloorRightPath, RightSpriteWidth);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");

            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Require(artRoot != null, "Art Dressing - Preserve is missing.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing.");

            foreach (Vector3Int cell in EnumerateArmorCells())
            {
                Require(collision.HasTile(cell),
                    "Expected hidden safety-floor collision is missing at " + cell + ".");
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
            managedRoot.transform.position = Vector3.zero;
            managedRoot.transform.localScale = Vector3.one;

            CreateSimplePiece(managedRoot.transform, "Safety Floor Armor - Left", leftSprite, material, LeftPosition);
            CreateTiledPiece(managedRoot.transform, "Safety Floor Armor - Mid", midSprite, material, MidPosition, MidSize);
            CreateSimplePiece(managedRoot.transform, "Safety Floor Armor - Right", rightSprite, material, RightPosition);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying safety-floor armor.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void CreateSimplePiece(
            Transform parent,
            string name,
            Sprite sprite,
            Material material,
            Vector2 position)
        {
            GameObject piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.position = new Vector3(position.x, position.y, 0f);
            piece.transform.localScale = Vector3.one;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;
        }

        private static void CreateTiledPiece(
            Transform parent,
            string name,
            Sprite sprite,
            Material material,
            Vector2 position,
            Vector2 size)
        {
            GameObject piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.position = new Vector3(position.x, position.y, 0f);
            piece.transform.localScale = Vector3.one;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
        }

        private static Sprite RequireProductionSprite(string assetPath, int expectedSpriteWidth)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Require(sprite != null, "Missing production sprite: " + assetPath);
            Require(Mathf.Approximately(sprite.pixelsPerUnit, PixelsPerUnit),
                assetPath + " must import at 16 PPU.");
            Require(Mathf.RoundToInt(sprite.rect.width) == expectedSpriteWidth &&
                Mathf.RoundToInt(sprite.rect.height) == SpriteHeight,
                assetPath + " imported Sprite rect changed unexpectedly. Expected " +
                expectedSpriteWidth + "x" + SpriteHeight + ", got " +
                Mathf.RoundToInt(sprite.rect.width) + "x" + Mathf.RoundToInt(sprite.rect.height) + ".");
            Require(Mathf.Approximately(sprite.pivot.x, 0f) && Mathf.Approximately(sprite.pivot.y, 0f),
                assetPath + " must keep its accepted bottom-left Sprite pivot.");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for production sprite: " + assetPath);
            Require(importer.filterMode == FilterMode.Point,
                assetPath + " must use Point filtering.");
            Require(!importer.mipmapEnabled,
                assetPath + " must keep mipmaps disabled.");

            importer.GetSourceTextureWidthAndHeight(out int actualWidth, out int actualHeight);
            Require(actualWidth == SourceTextureWidth && actualHeight == SourceTextureHeight,
                assetPath + " source dimensions changed unexpectedly. Expected " +
                SourceTextureWidth + "x" + SourceTextureHeight + ", got " +
                actualWidth + "x" + actualHeight + ".");
            return sprite;
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Safety-floor armor validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Transform artRoot = root?.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Safety Floor Armor v0 managed root is missing.");
            Require(managedRoot.localScale == Vector3.one,
                "Safety-floor armor root must remain at native scale 1.0.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Safety-floor armor must remain visual-only and contain no Collider2D.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing.");

            foreach (Vector3Int cell in EnumerateArmorCells())
            {
                Require(!structure.HasTile(cell),
                    "A breachable visual RuleTile remains inside the safety-floor armor envelope at " + cell + ".");
                Require(collision.HasTile(cell),
                    "Safety-floor armor must preserve hidden collision at " + cell + ".");
            }

            Require(managedRoot.childCount == 3,
                "Safety-floor armor must contain exactly left, tiled-middle, and right renderers.");

            ValidateSimplePiece(managedRoot, "Safety Floor Armor - Left", FloorLeftPath, LeftSpriteWidth, LeftPosition);
            ValidateTiledPiece(managedRoot, "Safety Floor Armor - Mid", FloorMidPath, MidSpriteWidth, MidPosition, MidSize);
            ValidateSimplePiece(managedRoot, "Safety Floor Armor - Right", FloorRightPath, RightSpriteWidth, RightPosition);
        }

        private static void ValidateSimplePiece(
            Transform root,
            string name,
            string spritePath,
            int expectedSpriteWidth,
            Vector2 expectedPosition)
        {
            Transform child = root.Find(name);
            Require(child != null, "Missing safety-floor armor piece: " + name);
            Require(child.localScale == Vector3.one,
                name + " must remain at native scale 1.0.");
            Require(Mathf.Approximately(child.position.x, expectedPosition.x) &&
                Mathf.Approximately(child.position.y, expectedPosition.y),
                name + " moved away from the armor envelope contract.");

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            Require(renderer != null &&
                renderer.sprite == RequireProductionSprite(spritePath, expectedSpriteWidth) &&
                renderer.sortingOrder == SortingOrder &&
                renderer.drawMode == SpriteDrawMode.Simple,
                name + " renderer contract is invalid.");
        }

        private static void ValidateTiledPiece(
            Transform root,
            string name,
            string spritePath,
            int expectedSpriteWidth,
            Vector2 expectedPosition,
            Vector2 expectedSize)
        {
            Transform child = root.Find(name);
            Require(child != null, "Missing safety-floor armor piece: " + name);
            Require(child.localScale == Vector3.one,
                name + " must remain at native scale 1.0.");
            Require(Mathf.Approximately(child.position.x, expectedPosition.x) &&
                Mathf.Approximately(child.position.y, expectedPosition.y),
                name + " moved away from the armor envelope contract.");

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            Require(renderer != null &&
                renderer.sprite == RequireProductionSprite(spritePath, expectedSpriteWidth) &&
                renderer.sortingOrder == SortingOrder &&
                renderer.drawMode == SpriteDrawMode.Tiled &&
                renderer.size == expectedSize,
                name + " tiled renderer contract is invalid.");
        }

        private static System.Collections.Generic.IEnumerable<Vector3Int> EnumerateArmorCells()
        {
            for (int x = FloorLeftCell; x < FloorRightExclusiveCell; x++)
            {
                for (int y = ArmorBottomCell; y < ArmorTopExclusiveCell; y++)
                {
                    yield return new Vector3Int(x, y, 0);
                }
            }
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