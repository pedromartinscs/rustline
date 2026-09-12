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
    /// Migrates an already-dressed SalvageIntake scene to the current handler-overhead geometry
    /// without rebuilding the room or disturbing preserved production art.
    /// </summary>
    public static class RustlineSalvageIntakeHandlerOverheadSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string CollisionTilePath =
            "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";

        private const int OldLeft = -8;
        private const int OldBottom = 18;
        private const int OldWidth = 16;
        private const int NewLeft = -8;
        private const int NewBottom = 19;
        private const int NewWidth = 33;

        [MenuItem("Tools/Rustline/Apply Salvage Intake Handler Overhead Support")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Handler overhead support raised to y=19 and extended east through x=24.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Handler Overhead Support")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Handler overhead support validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Require(ruleTile != null && collisionTile != null,
                "Handler-overhead support dependencies are missing.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake structural Tilemaps are missing.");

            ClearHorizontalStrip(structure, OldLeft, OldBottom, OldWidth);
            ClearHorizontalStrip(collision, OldLeft, OldBottom, OldWidth);
            ClearHorizontalStrip(structure, NewLeft, NewBottom, NewWidth);
            ClearHorizontalStrip(collision, NewLeft, NewBottom, NewWidth);

            SetHorizontalStrip(structure, NewLeft, NewBottom, NewWidth, ruleTile);
            SetHorizontalStrip(collision, NewLeft, NewBottom, NewWidth, collisionTile);

            structure.RefreshAllTiles();
            collision.RefreshAllTiles();
            TilemapCompositeColliderInitializer2D initializer =
                collision.GetComponent<TilemapCompositeColliderInitializer2D>();
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            Require(initializer != null && composite != null,
                "SalvageIntake collision bake components are missing.");
            initializer.EnsureGeometry();
            Require(composite.pathCount > 0 && composite.pointCount > 0,
                "SalvageIntake collision geometry became empty after moving the handler support.");

            EditorUtility.SetDirty(structure);
            EditorUtility.SetDirty(collision);
            EditorUtility.SetDirty(composite);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after moving the handler overhead support.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Handler-overhead validation requires the saved SalvageIntake scene.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(ruleTile != null && collisionTile != null && structure != null && collision != null,
                "Handler-overhead validation dependencies are incomplete.");

            for (int x = OldLeft; x < OldLeft + OldWidth; x++)
            {
                Require(!structure.HasTile(new Vector3Int(x, OldBottom, 0)) &&
                    !collision.HasTile(new Vector3Int(x, OldBottom, 0)),
                    "Old handler-overhead support remains at x=" + x + ", y=" + OldBottom + ".");
            }

            for (int x = NewLeft; x < NewLeft + NewWidth; x++)
            {
                Vector3Int cell = new Vector3Int(x, NewBottom, 0);
                Require(structure.GetTile(cell) == ruleTile && collision.GetTile(cell) == collisionTile,
                    "Handler-overhead support is incomplete at " + cell + ".");
            }

            // Keep one full cell of visual/physical separation before the shaft wall starts at x=26.
            Vector3Int reservedGap = new Vector3Int(25, NewBottom, 0);
            Require(!structure.HasTile(reservedGap) && !collision.HasTile(reservedGap),
                "Handler-overhead support must stop at x=24 and leave x=25 open for the future transition.");
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
