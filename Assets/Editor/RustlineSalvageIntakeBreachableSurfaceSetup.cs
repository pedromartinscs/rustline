using System;
using Rustline.Gameplay.Environment;
using Rustline.Gameplay.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Wires the runtime breaching authority onto Salvage Intake's existing hidden Ground Tilemap.
    /// Geometry remains owned by the normal production passes; this setup only establishes the
    /// explicit visual/collision/weapon references used when play-mode hits mutate the structure.
    /// </summary>
    public static class RustlineSalvageIntakeBreachableSurfaceSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string LongwatchPath = "Assets/Config/Weapons/LongwatchDMR.asset";

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Run the production setup first.");

            WeaponDefinition2D longwatch = AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LongwatchPath);
            Require(longwatch != null, "LongwatchDMR.asset is missing.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "Salvage Intake structural Tilemaps are missing.");

            Require(collision.GetComponent<TilemapCollider2D>() != null &&
                    collision.GetComponent<CompositeCollider2D>() != null,
                "Ground Collision - Hidden must keep the accepted Tilemap/Composite collider stack.");

            BreachableTilemap2D receiver = collision.GetComponent<BreachableTilemap2D>();
            if (receiver == null)
            {
                receiver = collision.gameObject.AddComponent<BreachableTilemap2D>();
            }

            receiver.Configure(structure, longwatch);
            EditorUtility.SetDirty(receiver);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after wiring structural breaching.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene, longwatch);
        }

        private static void ValidateScene(Scene scene, WeaponDefinition2D longwatch)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Breachable-surface validation requires the saved SalvageIntake scene.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "Salvage Intake structural Tilemaps are missing during breaching validation.");

            BreachableTilemap2D[] receivers = collision.GetComponents<BreachableTilemap2D>();
            Require(receivers.Length == 1,
                "Ground Collision - Hidden must contain exactly one BreachableTilemap2D authority.");

            BreachableTilemap2D receiver = receivers[0];
            Require(receiver.StructuralVisualTilemap == structure,
                "BreachableTilemap2D must target Industrial Surface - Visual.");
            Require(receiver.BreachingWeaponCount == 1 && receiver.AllowsWeapon(longwatch),
                "Longwatch DMR must be the sole authorized breaching weapon in Salvage Intake.");

            Require(collision.GetComponent<TilemapCollider2D>() != null &&
                    collision.GetComponent<CompositeCollider2D>() != null,
                "Breaching setup must not replace the accepted hidden collision stack.");
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
