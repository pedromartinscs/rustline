using System;
using System.Linq;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    public static class RustlineVisualRefinementSetup
    {
        private const string MaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [UnityEditor.MenuItem("Tools/Rustline/Visual Direction/Apply Phase 2 Refinement")]
        public static void Apply()
        {
            RustlineEnvironmentVisualHardeningSetup.ApplyHorizontalFloor();
            Scene scene = SceneManager.GetActiveScene();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach (CombatHealth2D health in Enemies(scene)) ConfigureEnemy(health, material);
            ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save phase 2.");
            Debug.Log("RUSTLINE_VISUAL_PHASE2_OK: original I-beams restored; background preserved; fourteen conditional enemy bars.");
        }

        public static void ConfigureEnemy(CombatHealth2D health, Material material)
        {
            var bar = health.GetComponent<EnemyHealthBar2D>();
            if (bar == null) bar = health.gameObject.AddComponent<EnemyHealthBar2D>();
            var serialized = new SerializedObject(bar);
            serialized.FindProperty("barMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityEditor.MenuItem("Tools/Rustline/Visual Direction/Validate Phase 2 Refinement")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(RustlineFirstPlayableSetup.ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            RustlineFirstPlayableSetup.ValidateScene(scene);
            Debug.Log("RUSTLINE_VISUAL_PHASE2_VALIDATED: beams, background, fourteen enemy bars and original production contracts.");
        }

        public static void ValidateScene(Scene scene)
        {
            RustlineVisualDirectionSliceSetup.ValidateScene(scene);
            RustlineEnvironmentVisualHardeningSetup.ValidateScene(scene);
            CombatHealth2D[] enemies = Enemies(scene);
            if (enemies.Length != 14) throw new InvalidOperationException("Expected fourteen existing production enemies.");
            Material expected = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            foreach (CombatHealth2D health in enemies)
            {
                var bars = health.GetComponents<EnemyHealthBar2D>();
                if (bars.Length != 1 || !bars[0].enabled ||
                    new SerializedObject(bars[0]).FindProperty("barMaterial").objectReferenceValue != expected)
                    throw new InvalidOperationException("Missing/invalid enemy health bar: " + health.name);
            }
        }

        private static CombatHealth2D[] Enemies(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<CombatHealth2D>(true))
            .Where(h => h.GetComponent<Sentry2D>() != null || h.GetComponent<Bombardier2D>() != null).ToArray();
    }
}
