using System;
using Rustline.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the current route-level structural refinements after the canonical SalvageIntake
    /// graybox rebuild. This pass deliberately keeps gameplay collision simple and deterministic:
    /// it raises the service-shaft exit, moves Exit Staging with it, and adds presentation-only
    /// background support columns beneath the starting deck.
    /// </summary>
    public static class RustlineSalvageIntakeRouteRefinementSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string BackgroundTilemapName = "Background Structure - Visual";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string ExitStagingName = "Exit Staging";
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string CollisionTilePath =
            "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";

        private const int RightWallLeft = 32;
        private const int RightWallWidth = 2;
        private const int PreviousRightWallTopRow = 14;
        private const int RaisedRightWallTopRow = 16;

        private const int ContinuationDeckLeft = 34;
        private const int ContinuationDeckWidth = 12;
        private const int PreviousContinuationDeckRow = 14;
        private const int RaisedContinuationDeckRow = 16;

        private static readonly Vector3 ExitStagingPosition = new Vector3(42f, 17.08f, 0f);

        // Visual-only supports below the initial west deck. Their tops meet the underside of the
        // managed safety-floor mass at y=-4; the bottoms continue below the initial camera frame so
        // the starting platform reads as part of a larger industrial structure rather than floating.
        private static readonly CellRect[] EntrySupportColumns =
        {
            new CellRect(-23, -12, 2, 8),
            new CellRect(-19, -12, 2, 8),
        };

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

        [MenuItem("Tools/Rustline/Apply Salvage Intake Route Refinements")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Route refinements applied: shaft exit raised to y=17 and west entry support columns added.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Route Refinements")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Route-refinement validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Require(ruleTile != null && collisionTile != null,
                "SalvageIntake route-refinement tile dependencies are missing.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap background = FindTilemap(scene, BackgroundTilemapName);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(background != null && structure != null && collision != null,
                "SalvageIntake managed Tilemaps are missing.");

            // Remove the previous continuation deck and clear the two new wall/deck rows so this
            // pass remains idempotent when Apply Production Setup is run repeatedly.
            ClearHorizontalStrip(structure, ContinuationDeckLeft, PreviousContinuationDeckRow, ContinuationDeckWidth);
            ClearHorizontalStrip(collision, ContinuationDeckLeft, PreviousContinuationDeckRow, ContinuationDeckWidth);
            ClearHorizontalStrip(structure, ContinuationDeckLeft, RaisedContinuationDeckRow, ContinuationDeckWidth);
            ClearHorizontalStrip(collision, ContinuationDeckLeft, RaisedContinuationDeckRow, ContinuationDeckWidth);

            for (int y = PreviousRightWallTopRow + 1; y <= RaisedRightWallTopRow; y++)
            {
                ClearHorizontalStrip(structure, RightWallLeft, y, RightWallWidth);
                ClearHorizontalStrip(collision, RightWallLeft, y, RightWallWidth);
            }

            SetHorizontalStrip(structure, ContinuationDeckLeft, RaisedContinuationDeckRow, ContinuationDeckWidth, ruleTile);
            SetHorizontalStrip(collision, ContinuationDeckLeft, RaisedContinuationDeckRow, ContinuationDeckWidth, collisionTile);
            for (int y = PreviousRightWallTopRow + 1; y <= RaisedRightWallTopRow; y++)
            {
                SetHorizontalStrip(structure, RightWallLeft, y, RightWallWidth, ruleTile);
                SetHorizontalStrip(collision, RightWallLeft, y, RightWallWidth, collisionTile);
            }

            foreach (CellRect column in EntrySupportColumns)
            {
                ClearRect(background, column);
                SetRect(background, column, ruleTile);
            }

            GameObject exit = FindGameObject(scene, ExitStagingName);
            Require(exit != null, "Exit Staging marker is missing from SalvageIntake.");
            exit.transform.position = ExitStagingPosition;

            background.RefreshAllTiles();
            structure.RefreshAllTiles();
            collision.RefreshAllTiles();

            TilemapCompositeColliderInitializer2D initializer =
                collision.GetComponent<TilemapCompositeColliderInitializer2D>();
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            Require(initializer != null && composite != null,
                "SalvageIntake collision bake components are missing.");
            initializer.EnsureGeometry();
            Require(composite.pathCount > 0 && composite.pointCount > 0,
                "SalvageIntake collision geometry became empty after route refinement.");

            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(structure);
            EditorUtility.SetDirty(collision);
            EditorUtility.SetDirty(exit);
            EditorUtility.SetDirty(composite);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying route refinements.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Route-refinement validation requires the saved SalvageIntake scene.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Tilemap background = FindTilemap(scene, BackgroundTilemapName);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(ruleTile != null && collisionTile != null &&
                background != null && structure != null && collision != null,
                "Route-refinement validation dependencies are incomplete.");

            for (int x = ContinuationDeckLeft; x < ContinuationDeckLeft + ContinuationDeckWidth; x++)
            {
                Vector3Int oldCell = new Vector3Int(x, PreviousContinuationDeckRow, 0);
                Vector3Int newCell = new Vector3Int(x, RaisedContinuationDeckRow, 0);
                Require(!structure.HasTile(oldCell) && !collision.HasTile(oldCell),
                    "Old upper continuation deck remains at " + oldCell + ".");
                Require(structure.GetTile(newCell) == ruleTile && collision.GetTile(newCell) == collisionTile,
                    "Raised upper continuation deck is incomplete at " + newCell + ".");
            }

            for (int y = 0; y <= RaisedRightWallTopRow; y++)
            {
                for (int x = RightWallLeft; x < RightWallLeft + RightWallWidth; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    Require(collision.GetTile(cell) == collisionTile,
                        "Raised right shaft wall collision is incomplete at " + cell + ".");
                }
            }

            // Preserve the accepted 64 px interior all the way through the raised exit region.
            for (int x = 28; x <= 31; x++)
            {
                for (int y = 0; y <= RaisedContinuationDeckRow; y++)
                {
                    Require(!collision.HasTile(new Vector3Int(x, y, 0)),
                        "Route refinement intrudes into the playable 64 px shaft interior.");
                }
            }

            for (int x = 26; x <= 27; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    Require(!collision.HasTile(new Vector3Int(x, y, 0)),
                        "Route refinement closed the accepted 4 u lower shaft entrance.");
                }
            }

            foreach (CellRect column in EntrySupportColumns)
            {
                for (int x = column.Left; x < column.Left + column.Width; x++)
                {
                    for (int y = column.Bottom; y < column.Bottom + column.Height; y++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, 0);
                        Require(background.GetTile(cell) == ruleTile,
                            "West entry support column is incomplete at " + cell + ".");
                        Require(!collision.HasTile(cell),
                            "West entry support columns must remain presentation-only at " + cell + ".");
                    }
                }
            }

            GameObject exit = FindGameObject(scene, ExitStagingName);
            Require(exit != null && exit.transform.position == ExitStagingPosition,
                "Exit Staging must follow the raised shaft exit to (42, 17.08, 0).");
        }

        private static void ClearHorizontalStrip(Tilemap tilemap, int left, int y, int width)
        {
            for (int x = left; x < left + width; x++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }

        private static void SetHorizontalStrip(Tilemap tilemap, int left, int y, int width, TileBase tile)
        {
            for (int x = left; x < left + width; x++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        private static void ClearRect(Tilemap tilemap, CellRect rect)
        {
            for (int x = rect.Left; x < rect.Left + rect.Width; x++)
            {
                for (int y = rect.Bottom; y < rect.Bottom + rect.Height; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }

        private static void SetRect(Tilemap tilemap, CellRect rect, TileBase tile)
        {
            for (int x = rect.Left; x < rect.Left + rect.Width; x++)
            {
                for (int y = rect.Bottom; y < rect.Bottom + rect.Height; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static Tilemap FindTilemap(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.name == name)
                    {
                        return tilemap;
                    }
                }
            }
            return null;
        }

        private static GameObject FindGameObject(Scene scene, string name)
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
