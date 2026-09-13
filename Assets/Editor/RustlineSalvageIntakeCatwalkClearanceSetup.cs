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
    /// Raises the production transfer catwalk by one structural cell after the canonical graybox
    /// and macro-art passes. The accepted player capsule physically fits beneath the historical
    /// y=3..4 strip, but the authored player silhouette extends slightly above that capsule and can
    /// visually intersect the catwalk underside. Moving both hidden collision and production art up
    /// exactly 1 u gives a full 64 px clear opening without changing frozen player geometry.
    /// </summary>
    public static class RustlineSalvageIntakeCatwalkClearanceSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string MacroRootName = "Macro Environment Kit v0 - Managed";
        private const string CatwalkLeftName = "Catwalk Skin - Left";
        private const string CatwalkRightName = "Catwalk Skin - Right";

        private const int CatwalkLeftCell = 4;
        private const int CatwalkRightExclusiveCell = 16;
        private const int HistoricalCollisionRow = 3;
        private const int ProductionCollisionRow = 4;
        private const float ProductionArtY = 4.84375f;
        private const float ProductionWalkableY = 5f;
        private const float FloorSurfaceY = 0f;
        private const float ExpectedUndersideClearance = 4f;

        [MenuItem("Tools/Rustline/Apply Salvage Intake Catwalk Clearance")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Transfer catwalk clearance was raised to 64 px while preserving its one-cell collision thickness.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Catwalk Clearance")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Transfer catwalk clearance validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Run Apply Production Setup first.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(collision != null, "Ground Collision - Hidden is missing.");

            for (int x = CatwalkLeftCell; x < CatwalkRightExclusiveCell; x++)
            {
                Vector3Int historical = new Vector3Int(x, HistoricalCollisionRow, 0);
                Vector3Int production = new Vector3Int(x, ProductionCollisionRow, 0);
                bool hasHistorical = collision.HasTile(historical);
                bool hasProduction = collision.HasTile(production);

                Require(hasHistorical || hasProduction,
                    "Catwalk collision is missing at both expected rows for x=" + x + ".");
                Require(!(hasHistorical && hasProduction),
                    "Catwalk collision occupies both historical and production rows at x=" + x + ".");

                if (hasHistorical)
                {
                    TileBase tile = collision.GetTile(historical);
                    Require(tile != null, "Catwalk collision tile could not be resolved at " + historical + ".");
                    collision.SetTile(historical, null);
                    collision.SetTile(production, tile);
                }
            }

            collision.RefreshAllTiles();
            EditorUtility.SetDirty(collision);

            Transform artRoot = FindTransform(scene, ArtDressingPreserveName);
            Transform macroRoot = artRoot?.Find(MacroRootName);
            Require(macroRoot != null,
                "Macro Environment Kit v0 - Managed is missing. Apply macro environment dressing first.");

            RaiseCatwalkPiece(macroRoot, CatwalkLeftName);
            RaiseCatwalkPiece(macroRoot, CatwalkRightName);

            TilemapCompositeColliderInitializer2D initializer =
                collision.GetComponent<TilemapCompositeColliderInitializer2D>();
            Require(initializer != null,
                "Ground Collision - Hidden is missing TilemapCompositeColliderInitializer2D.");
            initializer.EnsureGeometry();

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after raising catwalk clearance.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void RaiseCatwalkPiece(Transform macroRoot, string name)
        {
            Transform child = macroRoot.Find(name);
            Require(child != null, "Missing production catwalk sprite: " + name + ".");
            Require(child.localScale == Vector3.one,
                name + " must remain at native scale 1.0.");

            Vector3 position = child.position;
            child.position = new Vector3(position.x, ProductionArtY, position.z);
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Catwalk-clearance validation requires the saved SalvageIntake scene.");

            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(collision != null, "Ground Collision - Hidden is missing.");

            for (int x = CatwalkLeftCell; x < CatwalkRightExclusiveCell; x++)
            {
                Vector3Int historical = new Vector3Int(x, HistoricalCollisionRow, 0);
                Vector3Int production = new Vector3Int(x, ProductionCollisionRow, 0);
                Require(!collision.HasTile(historical),
                    "Historical catwalk collision remains at " + historical + ".");
                Require(collision.HasTile(production),
                    "Raised catwalk collision is missing at " + production + ".");
            }

            Transform artRoot = FindTransform(scene, ArtDressingPreserveName);
            Transform macroRoot = artRoot?.Find(MacroRootName);
            Require(macroRoot != null, "Macro Environment Kit v0 - Managed is missing.");
            ValidateCatwalkPiece(macroRoot, CatwalkLeftName);
            ValidateCatwalkPiece(macroRoot, CatwalkRightName);

            TilemapCollider2D tilemapCollider = collision.GetComponent<TilemapCollider2D>();
            CompositeCollider2D compositeCollider = collision.GetComponent<CompositeCollider2D>();
            TilemapCompositeColliderInitializer2D initializer =
                collision.GetComponent<TilemapCompositeColliderInitializer2D>();
            Require(tilemapCollider != null && tilemapCollider.enabled,
                "Raised catwalk requires the authoritative TilemapCollider2D.");
            Require(compositeCollider != null && compositeCollider.enabled &&
                compositeCollider.pathCount > 0 && compositeCollider.pointCount > 0,
                "Raised catwalk requires baked CompositeCollider2D geometry.");
            Require(initializer != null && initializer.HasGeneratedGeometry,
                "Raised catwalk collision initializer reports no generated geometry.");

            Require(Mathf.Approximately(ProductionCollisionRow - FloorSurfaceY, ExpectedUndersideClearance),
                "Catwalk underside clearance contract changed unexpectedly.");
            Require(Mathf.Approximately(ProductionWalkableY, ProductionCollisionRow + 1f),
                "Catwalk walkable surface must remain exactly one cell above its underside.");
        }

        private static void ValidateCatwalkPiece(Transform macroRoot, string name)
        {
            Transform child = macroRoot.Find(name);
            Require(child != null, "Missing production catwalk sprite: " + name + ".");
            Require(child.localScale == Vector3.one,
                name + " must remain at native scale 1.0.");
            Require(Mathf.Approximately(child.position.y, ProductionArtY),
                name + " must align its authored deck surface to y=" + ProductionWalkableY + ".");
            Require(child.GetComponent<Collider2D>() == null,
                name + " must remain visual-only; hidden Tilemap collision is authoritative.");
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform transform in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform;
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
