using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Creates a deliberate one-cell-wide protected micro-aperture in Salvage Intake before
    /// destructible terrain is accepted. The production graybox naturally leaves a two-cell gap
    /// between the raised left approach and the central machinery plinth; this pass fills only the
    /// west half visually, leaving x=-3 as a 16 px visual slot while preserving hidden collision.
    ///
    /// The fixture models the first-shot breaching state directly: structure may disappear, but a
    /// physically invalid 16 px opening must remain solid until enough adjacent runtime cells are
    /// destroyed to satisfy the minimum aperture rule.
    /// </summary>
    public static class RustlineSalvageIntakeMicroGapTestSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";

        private static readonly Vector3Int SourceBottom = new Vector3Int(-5, 0, 0);
        private static readonly Vector3Int SourceTop = new Vector3Int(-5, 1, 0);
        private static readonly Vector3Int ExtensionBottom = new Vector3Int(-4, 0, 0);
        private static readonly Vector3Int ExtensionTop = new Vector3Int(-4, 1, 0);
        private static readonly Vector3Int GapBottom = new Vector3Int(-3, 0, 0);
        private static readonly Vector3Int GapTop = new Vector3Int(-3, 1, 0);
        private static readonly Vector3Int RightBlockBottom = new Vector3Int(-2, 0, 0);
        private static readonly Vector3Int RightBlockTop = new Vector3Int(-2, 1, 0);
        private static readonly Vector3Int GapFloor = new Vector3Int(-3, -1, 0);

        [MenuItem("Tools/Rustline/Apply Salvage Intake Micro-Gap Test")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "The protected one-tile micro-aperture test was applied and validated.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Run the production setup first.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "Salvage Intake structural Tilemaps are missing.");

            TileBase structureBottom = structure.GetTile(SourceBottom);
            TileBase structureTop = structure.GetTile(SourceTop);
            TileBase collisionBottom = collision.GetTile(SourceBottom);
            TileBase collisionTop = collision.GetTile(SourceTop);
            Require(structureBottom != null && structureTop != null,
                "The accepted left-approach visual source cells are missing.");
            Require(collisionBottom != null && collisionTop != null,
                "The accepted left-approach collision source cells are missing.");

            // Extend the west block by exactly one column. The x=-3 cells deliberately have no
            // structural visual but retain collision, representing a destroyed micro-aperture that
            // is too narrow to become a real traversal opening.
            structure.SetTile(ExtensionBottom, structureBottom);
            structure.SetTile(ExtensionTop, structureTop);
            collision.SetTile(ExtensionBottom, collisionBottom);
            collision.SetTile(ExtensionTop, collisionTop);

            structure.SetTile(GapBottom, null);
            structure.SetTile(GapTop, null);
            collision.SetTile(GapBottom, collisionBottom);
            collision.SetTile(GapTop, collisionTop);

            structure.RefreshAllTiles();
            collision.RefreshAllTiles();
            BakeCollision(collision);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying the protected micro-gap test.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Micro-gap validation requires the saved SalvageIntake scene.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "Salvage Intake structural Tilemaps are missing during validation.");

            Require(structure.HasTile(ExtensionBottom) && structure.HasTile(ExtensionTop),
                "The west side of the micro-gap must be filled visually at x=-4, y=0..1.");
            Require(collision.HasTile(ExtensionBottom) && collision.HasTile(ExtensionTop),
                "The west side of the micro-gap must be physically solid at x=-4, y=0..1.");

            Require(!structure.HasTile(GapBottom) && !structure.HasTile(GapTop),
                "The diagnostic slot must remain visually empty at x=-3, y=0..1.");
            Require(collision.HasTile(GapBottom) && collision.HasTile(GapTop),
                "The 16 px diagnostic micro-aperture must preserve hidden collision at x=-3, y=0..1.");

            Require(structure.HasTile(RightBlockBottom) && structure.HasTile(RightBlockTop),
                "The central plinth must still bound the micro-gap on the east side.");
            Require(collision.HasTile(RightBlockBottom) && collision.HasTile(RightBlockTop),
                "The central plinth collision must still bound the micro-gap on the east side.");
            Require(collision.HasTile(GapFloor),
                "The protected base floor must remain directly beneath the one-tile slot.");

            TilemapCollider2D tilemapCollider = collision.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            Require(tilemapCollider != null && composite != null,
                "The hidden Ground Tilemap must keep its TilemapCollider2D and CompositeCollider2D.");
            Require(composite.pathCount > 0 && composite.pointCount > 0,
                "Composite collision geometry must remain generated after the micro-gap mutation.");
        }

        private static void BakeCollision(Tilemap collision)
        {
            TilemapCollider2D tilemapCollider = collision.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            Require(tilemapCollider != null && composite != null,
                "The hidden Ground Tilemap must keep its accepted collider stack.");

            tilemapCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            Physics2D.SyncTransforms();
        }

        private static Tilemap FindTilemap(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.name == objectName)
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
