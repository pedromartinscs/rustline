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

        // Historical first graybox support.
        private const int LegacyLeft = -8;
        private const int LegacyBottom = 18;
        private const int LegacyWidth = 16;

        // Previous production support: y=19, x=-8..24.
        private const int PreviousLeft = -8;
        private const int PreviousBottom = 19;
        private const int PreviousWidth = 33;

        // Current production support. Raising the main span to y=20 gives the crane/handler more
        // vertical breathing room. Extending through x=27 reaches exactly across the left service-
        // shaft wall mass without entering the playable shaft interior at x=28..31.
        private const int CurrentLeft = -8;
        private const int CurrentBottom = 20;
        private const int CurrentWidth = 36; // x=-8..27 inclusive.

        // A two-cell structural key grows one tile above the accepted shaft wall (x=26..27,
        // y=4..18), joining its y=19 top extension directly into the raised overhead span.
        private const int ShaftJointLeft = 26;
        private const int ShaftJointBottom = 19;
        private const int ShaftJointWidth = 2;

        [MenuItem("Tools/Rustline/Apply Salvage Intake Handler Overhead Support")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Handler overhead support raised to y=20, extended through x=27, and keyed into the left service-shaft wall.",
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

            // Clear every historical support span that this migration has owned. The shaft wall
            // itself ends at y=18, so clearing the y=19 joint cells cannot erase canonical wall data.
            ClearHorizontalStrip(structure, LegacyLeft, LegacyBottom, LegacyWidth);
            ClearHorizontalStrip(collision, LegacyLeft, LegacyBottom, LegacyWidth);
            ClearHorizontalStrip(structure, PreviousLeft, PreviousBottom, PreviousWidth);
            ClearHorizontalStrip(collision, PreviousLeft, PreviousBottom, PreviousWidth);
            ClearHorizontalStrip(structure, CurrentLeft, CurrentBottom, CurrentWidth);
            ClearHorizontalStrip(collision, CurrentLeft, CurrentBottom, CurrentWidth);
            ClearHorizontalStrip(structure, ShaftJointLeft, ShaftJointBottom, ShaftJointWidth);
            ClearHorizontalStrip(collision, ShaftJointLeft, ShaftJointBottom, ShaftJointWidth);

            SetHorizontalStrip(structure, CurrentLeft, CurrentBottom, CurrentWidth, ruleTile);
            SetHorizontalStrip(collision, CurrentLeft, CurrentBottom, CurrentWidth, collisionTile);
            SetHorizontalStrip(structure, ShaftJointLeft, ShaftJointBottom, ShaftJointWidth, ruleTile);
            SetHorizontalStrip(collision, ShaftJointLeft, ShaftJointBottom, ShaftJointWidth, collisionTile);

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

            for (int x = LegacyLeft; x < LegacyLeft + LegacyWidth; x++)
            {
                Require(!structure.HasTile(new Vector3Int(x, LegacyBottom, 0)) &&
                    !collision.HasTile(new Vector3Int(x, LegacyBottom, 0)),
                    "Legacy handler-overhead support remains at x=" + x + ", y=" + LegacyBottom + ".");
            }

            for (int x = PreviousLeft; x < PreviousLeft + PreviousWidth; x++)
            {
                Require(!structure.HasTile(new Vector3Int(x, PreviousBottom, 0)) &&
                    !collision.HasTile(new Vector3Int(x, PreviousBottom, 0)),
                    "Previous handler-overhead support remains at x=" + x + ", y=" + PreviousBottom + ".");
            }

            for (int x = CurrentLeft; x < CurrentLeft + CurrentWidth; x++)
            {
                Vector3Int cell = new Vector3Int(x, CurrentBottom, 0);
                Require(structure.GetTile(cell) == ruleTile && collision.GetTile(cell) == collisionTile,
                    "Handler-overhead support is incomplete at " + cell + ".");
            }

            for (int x = ShaftJointLeft; x < ShaftJointLeft + ShaftJointWidth; x++)
            {
                Vector3Int joint = new Vector3Int(x, ShaftJointBottom, 0);
                Vector3Int wallBelow = new Vector3Int(x, ShaftJointBottom - 1, 0);
                Require(structure.GetTile(joint) == ruleTile && collision.GetTile(joint) == collisionTile,
                    "Handler/shaft structural joint is incomplete at " + joint + ".");
                Require(collision.HasTile(wallBelow),
                    "Left shaft wall no longer reaches the handler/shaft joint at " + wallBelow + ".");
            }

            // The new connection must never cross the accepted x=28 inner face of the left wall.
            // Keep both the joint row and raised support row empty throughout the playable 64 px shaft.
            for (int x = 28; x <= 31; x++)
            {
                Vector3Int jointRowCell = new Vector3Int(x, ShaftJointBottom, 0);
                Vector3Int supportRowCell = new Vector3Int(x, CurrentBottom, 0);
                Require(!structure.HasTile(jointRowCell) && !collision.HasTile(jointRowCell),
                    "Handler/shaft joint intrudes into the playable shaft at " + jointRowCell + ".");
                Require(!structure.HasTile(supportRowCell) && !collision.HasTile(supportRowCell),
                    "Raised handler support intrudes into the playable shaft at " + supportRowCell + ".");
            }
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
