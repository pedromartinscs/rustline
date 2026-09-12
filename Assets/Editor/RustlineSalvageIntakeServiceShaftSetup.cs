using System;
using System.Collections.Generic;
using Rustline.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the first production-layout extension to SalvageIntake: a four-unit-wide vertical
    /// service shaft at the east edge. This pass is deliberately separate from the canonical graybox
    /// builder until human playtesting approves the geometry. Hidden Tilemap collision remains the
    /// sole gameplay authority.
    /// </summary>
    public static class RustlineSalvageIntakeServiceShaftSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string CollisionTilePath =
            "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";
        private const string ExitStagingName = "Exit Staging";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string WallSkinManagedRootName = "Structural Wall Skin v0 - Managed";

        // Reserve only the east extension region so the accepted west/hero-room composition and all
        // production skins elsewhere remain untouched. X 26..45, Y -4..18 is owned by this test pass.
        private const int ReservedLeft = 26;
        private const int ReservedBottom = -4;
        private const int ReservedWidth = 20;
        private const int ReservedHeight = 23;

        // Geometry contract:
        // - the old east boundary becomes the upper left shaft wall, with a 4 u / 64 px entry below it;
        // - four open cells x=28..31 reproduce the comfortable MovementLab wall-kick spacing;
        // - the right shaft wall tops out at y=15, becoming a natural ledge onto the upper exit deck;
        // - the left wall continues to the room's y=19 envelope, encouraging alternating wall kicks;
        // - the exit deck opens the level toward the east for the next production section.
        private static readonly CellRect[] ShaftStructureRects =
        {
            new CellRect(26, -4, 8, 4),   // safety floor under entry + shaft
            new CellRect(26, 4, 2, 15),   // left shaft wall, y=4..18
            new CellRect(32, 0, 2, 15),   // right shaft wall, y=0..14, top surface at y=15
            new CellRect(34, 14, 12, 1),  // upper-right continuation deck, top surface at y=15
        };

        private static readonly Vector3 ExitStagingPosition = new Vector3(42f, 15.08f, 0f);

        private readonly struct CellRect
        {
            internal CellRect(int left, int bottom, int width, int height)
            {
                Left = left;
                Bottom = bottom;
                Width = width;
                Height = height;
            }

            internal int Left { get; }
            internal int Bottom { get; }
            internal int Width { get; }
            internal int Height { get; }
        }

        [MenuItem("Tools/Rustline/Apply Salvage Intake Service Shaft")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "East service shaft test pass was applied. Shaft width is 4 u / 64 px and the upper exit now continues east.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Service Shaft")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the graybox first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "East service shaft validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the graybox first.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Require(ruleTile != null && collisionTile != null,
                "SalvageIntake service-shaft dependencies are missing.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing. Rebuild the graybox first.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing.");

            ClearReservedRegion(structure);
            ClearReservedRegion(collision);

            foreach (Vector3Int cell in CollectCells(ShaftStructureRects))
            {
                structure.SetTile(cell, ruleTile);
                collision.SetTile(cell, collisionTile);
            }

            structure.RefreshAllTiles();
            collision.RefreshAllTiles();
            EditorUtility.SetDirty(structure);
            EditorUtility.SetDirty(collision);

            // The former full-height east boundary wall skin would visually seal the new entry/shaft.
            // Preserve the approved west wall skin while removing only the now-obsolete east children.
            RemoveObsoleteEastBoundarySkin(root.transform);

            GameObject exit = FindGameObject(scene, ExitStagingName);
            Require(exit != null, "Exit Staging marker is missing. Rebuild the graybox first.");
            exit.transform.position = ExitStagingPosition;
            EditorUtility.SetDirty(exit.transform);

            BakeCollision(collision);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying the service shaft.");
            AssetDatabase.SaveAssets();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Service-shaft validation requires the saved SalvageIntake scene.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Require(structure != null && collision != null && ruleTile != null && collisionTile != null,
                "Service-shaft Tilemap dependencies are incomplete.");

            HashSet<Vector3Int> expected = new HashSet<Vector3Int>(CollectCells(ShaftStructureRects));
            foreach (Vector3Int cell in EnumerateReservedRegion())
            {
                if (expected.Contains(cell))
                {
                    Require(structure.GetTile(cell) == ruleTile,
                        "Service-shaft visual structure is missing at " + cell + ".");
                    Require(collision.GetTile(cell) == collisionTile,
                        "Service-shaft hidden collision is missing at " + cell + ".");
                }
                else
                {
                    Require(!structure.HasTile(cell),
                        "Unexpected visual structure remains inside the reserved service-shaft region at " + cell + ".");
                    Require(!collision.HasTile(cell),
                        "Unexpected collision remains inside the reserved service-shaft region at " + cell + ".");
                }
            }

            // Explicit traversal checks: a 4 u-high entry and exactly four open shaft cells.
            for (int x = 26; x <= 27; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    Require(!collision.HasTile(new Vector3Int(x, y, 0)),
                        "The service-shaft lower entry must remain open for 4 full units.");
                }
            }
            for (int x = 28; x <= 31; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    Require(!collision.HasTile(new Vector3Int(x, y, 0)),
                        "The service-shaft interior must remain exactly four open cells wide.");
                }
            }

            GameObject exit = FindGameObject(scene, ExitStagingName);
            Require(exit != null && exit.transform.position == ExitStagingPosition,
                "Exit Staging must sit on the upper-right continuation deck.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform wallRoot = artRoot?.Find(WallSkinManagedRootName);
            if (wallRoot != null)
            {
                foreach (Transform child in wallRoot)
                {
                    Require(!child.name.StartsWith("Wall Skin - East", StringComparison.Ordinal),
                        "Obsolete full-height east wall skin still seals the service shaft.");
                }
            }

            BakeCollision(collision);
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            Require(composite != null && composite.pathCount > 0 && composite.pointCount > 0,
                "Service-shaft collision bake produced empty Composite geometry.");
        }

        private static void BakeCollision(Tilemap collision)
        {
            TilemapCollider2D tilemapCollider = collision.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            TilemapCompositeColliderInitializer2D initializer =
                collision.GetComponent<TilemapCompositeColliderInitializer2D>();
            Require(tilemapCollider != null && composite != null && initializer != null,
                "Service shaft requires the accepted Tilemap -> Composite collision pipeline.");

            collision.RefreshAllTiles();
            tilemapCollider.ProcessTilemapChanges();
            initializer.EnsureGeometry();
            Physics2D.SyncTransforms();
        }

        private static void RemoveObsoleteEastBoundarySkin(Transform root)
        {
            Transform artRoot = root.Find(ArtDressingPreserveName);
            Transform wallRoot = artRoot?.Find(WallSkinManagedRootName);
            if (wallRoot == null)
            {
                return;
            }

            for (int index = wallRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = wallRoot.GetChild(index);
                if (child.name.StartsWith("Wall Skin - East", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void ClearReservedRegion(Tilemap tilemap)
        {
            foreach (Vector3Int cell in EnumerateReservedRegion())
            {
                tilemap.SetTile(cell, null);
            }
        }

        private static IEnumerable<Vector3Int> EnumerateReservedRegion()
        {
            for (int x = ReservedLeft; x < ReservedLeft + ReservedWidth; x++)
            {
                for (int y = ReservedBottom; y < ReservedBottom + ReservedHeight; y++)
                {
                    yield return new Vector3Int(x, y, 0);
                }
            }
        }

        private static IEnumerable<Vector3Int> CollectCells(IEnumerable<CellRect> rects)
        {
            var cells = new HashSet<Vector3Int>();
            foreach (CellRect rect in rects)
            {
                Require(rect.Width > 0 && rect.Height > 0,
                    "Service-shaft CellRect dimensions must be positive.");
                for (int x = rect.Left; x < rect.Left + rect.Width; x++)
                {
                    for (int y = rect.Bottom; y < rect.Bottom + rect.Height; y++)
                    {
                        cells.Add(new Vector3Int(x, y, 0));
                    }
                }
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
