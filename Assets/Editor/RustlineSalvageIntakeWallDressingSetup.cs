using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the accepted production wall-edge skin to the permanent west boundary wall in
    /// SalvageIntake. The east side is intentionally not dressed by this family anymore: it now
    /// opens into the canonical service shaft and will receive shaft-specific structural art later.
    /// The sprites remain visual-only; hidden Tilemap collision stays authoritative.
    /// </summary>
    public static class RustlineSalvageIntakeWallDressingSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Structural Wall Skin v0 - Managed";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string WallTopPath =
            "Assets/Art/Environment/Architecture/Wall/wall_edge_top.png";
        private const string WallMidPath =
            "Assets/Art/Environment/Architecture/Wall/wall_edge_mid.png";
        private const string WallBottomPath =
            "Assets/Art/Environment/Architecture/Wall/wall_edge_bottom.png";

        private const int SourceWidth = 50;
        private const int SourceHeight = 150;
        private const int MidSpriteRectHeight = 150;
        private const int BottomSpriteRectHeight = 150;
        private const int TopSpriteRectHeight = 145;

        // The west visual inner face aligns exactly to collision x=-26. The extra source width
        // overhangs outside the playable room rather than intruding into traversal space.
        private const float WestWallX = -27.5625f;
        private const float BottomY = 4.6875f;
        private const float MidY = 9.65625f;
        private const float TopY = 14.46875f;

        private readonly struct WallPlacement
        {
            internal WallPlacement(
                string name,
                string assetPath,
                int expectedSpriteRectHeight,
                Vector2 position,
                int sortingOrder)
            {
                Name = name;
                AssetPath = assetPath;
                ExpectedSpriteRectHeight = expectedSpriteRectHeight;
                Position = position;
                SortingOrder = sortingOrder;
            }

            internal string Name { get; }
            internal string AssetPath { get; }
            internal int ExpectedSpriteRectHeight { get; }
            internal Vector2 Position { get; }
            internal int SortingOrder { get; }
        }

        private static readonly WallPlacement[] Placements =
        {
            new WallPlacement("Wall Skin - West Mid Fill", WallMidPath, MidSpriteRectHeight,
                new Vector2(WestWallX, MidY), 1),
            new WallPlacement("Wall Skin - West Bottom", WallBottomPath, BottomSpriteRectHeight,
                new Vector2(WestWallX, BottomY), 2),
            new WallPlacement("Wall Skin - West Top", WallTopPath, TopSpriteRectHeight,
                new Vector2(WestWallX, TopY), 2),
        };

        [MenuItem("Tools/Rustline/Apply Salvage Intake Wall Dressing")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "West boundary-wall dressing was applied at native scale. The east service shaft remains open.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Wall Dressing")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the graybox first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "West boundary-wall dressing validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the graybox first.");

            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "Sprite-Unlit-Default material is unavailable.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing. Rebuild the graybox first.");

            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Require(artRoot != null,
                "Art Dressing - Preserve is missing. Rebuild the SalvageIntake graybox first.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing. Rebuild the graybox first.");

            foreach (Vector3Int cell in EnumerateReplacedVisualCells())
            {
                Require(collision.HasTile(cell),
                    "Expected hidden west-wall collision is missing at " + cell + ".");
                structure.SetTile(cell, null);
            }
            structure.RefreshAllTiles();
            EditorUtility.SetDirty(structure);

            Transform oldRoot = artRoot.Find(ManagedRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            }

            GameObject managedRoot = new GameObject(ManagedRootName);
            managedRoot.transform.SetParent(artRoot, false);

            foreach (WallPlacement placement in Placements)
            {
                Sprite sprite = RequireProductionSprite(
                    placement.AssetPath,
                    placement.ExpectedSpriteRectHeight);

                GameObject spriteObject = new GameObject(placement.Name);
                spriteObject.transform.SetParent(managedRoot.transform, false);
                spriteObject.transform.position = new Vector3(placement.Position.x, placement.Position.y, 0f);
                spriteObject.transform.localScale = Vector3.one;

                SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = unlitMaterial;
                renderer.sortingOrder = placement.SortingOrder;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying wall dressing.");
            AssetDatabase.SaveAssets();
            ValidateScene(scene);
        }

        private static Sprite RequireProductionSprite(string assetPath, int expectedSpriteRectHeight)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Require(sprite != null, "Missing production sprite: " + assetPath);
            Require(Mathf.Approximately(sprite.pixelsPerUnit, 16f),
                assetPath + " must import at 16 PPU.");
            Require(Mathf.RoundToInt(sprite.rect.width) == SourceWidth &&
                Mathf.RoundToInt(sprite.rect.height) == expectedSpriteRectHeight,
                assetPath + " imported Sprite rect changed unexpectedly. Expected " +
                SourceWidth + "x" + expectedSpriteRectHeight + ", got " +
                Mathf.RoundToInt(sprite.rect.width) + "x" + Mathf.RoundToInt(sprite.rect.height) + ".");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for production sprite: " + assetPath);
            Require(importer.filterMode == FilterMode.Point,
                assetPath + " must use Point filtering.");
            Require(!importer.mipmapEnabled,
                assetPath + " must keep mipmaps disabled.");

            importer.GetSourceTextureWidthAndHeight(out int actualWidth, out int actualHeight);
            Require(actualWidth == SourceWidth && actualHeight == SourceHeight,
                assetPath + " source dimensions changed unexpectedly. Expected " +
                SourceWidth + "x" + SourceHeight + ", got " + actualWidth + "x" + actualHeight + ".");

            return sprite;
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Wall dressing validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Structural Wall Skin v0 managed root is missing.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing.");

            foreach (Vector3Int cell in EnumerateReplacedVisualCells())
            {
                Require(!structure.HasTile(cell),
                    "Generic visual wall tile was restored beneath production west-wall skin at " + cell + ".");
                Require(collision.HasTile(cell),
                    "Production west-wall skin must not remove hidden collision at " + cell + ".");
            }

            var renderers = new List<SpriteRenderer>(managedRoot.GetComponentsInChildren<SpriteRenderer>(true));
            Require(renderers.Count == Placements.Length,
                "Structural Wall Skin v0 must contain exactly the configured west-wall SpriteRenderers.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Wall dressing must remain non-colliding.");

            foreach (WallPlacement placement in Placements)
            {
                Transform child = managedRoot.Find(placement.Name);
                Require(child != null, "Missing wall dressing object: " + placement.Name);
                Require(child.localScale == Vector3.one,
                    placement.Name + " must remain at native scale 1.0.");
                Require(Mathf.Approximately(child.position.x, placement.Position.x) &&
                    Mathf.Approximately(child.position.y, placement.Position.y),
                    placement.Name + " moved away from the current wall composition contract.");

                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                Sprite expected = RequireProductionSprite(
                    placement.AssetPath,
                    placement.ExpectedSpriteRectHeight);
                Require(renderer != null && renderer.sprite == expected &&
                    renderer.sortingOrder == placement.SortingOrder && !renderer.flipX,
                    placement.Name + " renderer contract is invalid.");
            }
        }

        private static IEnumerable<Vector3Int> EnumerateReplacedVisualCells()
        {
            for (int y = 0; y < 19; y++)
            {
                yield return new Vector3Int(-28, y, 0);
                yield return new Vector3Int(-27, y, 0);
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