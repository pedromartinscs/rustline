using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the first production-environment macro art pass to SalvageIntake without touching
    /// gameplay collision. The generated dressing lives under Art Dressing - Preserve, so graybox
    /// rebuilds can continue to replace only the managed geometry/player roots.
    /// </summary>
    public static class RustlineSalvageIntakeArtSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string MacroRootName = "Macro Environment Kit v0 - Managed";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string GantryAPath =
            "Assets/Art/Environment/Machinery/gantry_upright_a.png";
        private const string GantryBPath =
            "Assets/Art/Environment/Machinery/gantry_upright_b.png";
        private const string HandlerPath =
            "Assets/Art/Environment/Machinery/salvage_handler.png";
        private const string HousingPath =
            "Assets/Art/Environment/Machinery/machine_housing.png";
        private const string BulkheadPath =
            "Assets/Art/Environment/Architecture/bulkhead_door_frame.png";

        private readonly struct DressingPlacement
        {
            internal DressingPlacement(
                string name,
                string assetPath,
                int sourceWidth,
                int sourceHeight,
                Vector2 position,
                int sortingOrder)
            {
                Name = name;
                AssetPath = assetPath;
                SourceWidth = sourceWidth;
                SourceHeight = sourceHeight;
                Position = position;
                SortingOrder = sortingOrder;
            }

            internal string Name { get; }
            internal string AssetPath { get; }
            internal int SourceWidth { get; }
            internal int SourceHeight { get; }
            internal Vector2 Position { get; }
            internal int SortingOrder { get; }
        }

        // Positions deliberately keep scale at 1.0: one source-art pixel remains one logical pixel.
        // Large machinery may extend below the gameplay floor, where the foreground structural
        // Tilemap naturally occludes it. This lets the pieces feel massive without rescaling art.
        private static readonly DressingPlacement[] Placements =
        {
            // The west bulkhead anchors directly on the gameplay floor: 200 px / 16 PPU = 12.5 u.
            new DressingPlacement(
                "Bulkhead Door Frame - West",
                BulkheadPath,
                210,
                200,
                new Vector2(-20f, 6.25f),
                -10),

            // Housing is intentionally sunk 3 u below floor level so only the useful upper mass
            // rises into the room behind the compact collision plinth.
            new DressingPlacement(
                "Machine Housing - Central",
                HousingPath,
                250,
                170,
                new Vector2(0f, 2.3125f),
                -16),

            // 300 px uprights are 18.75 u tall. Their centers put the visible top at y=16 while
            // the lowest 2.75 u disappear behind the floor, preserving native scale.
            new DressingPlacement(
                "Gantry Upright A - West",
                GantryAPath,
                100,
                300,
                new Vector2(-8f, 6.625f),
                -14),
            new DressingPlacement(
                "Gantry Upright B - East",
                GantryBPath,
                100,
                300,
                new Vector2(8f, 6.625f),
                -14),

            // The long suspension extends above the current room frame; the handler/claw remains
            // visible in the hero-space while still reading as something hanging from machinery.
            new DressingPlacement(
                "Salvage Handler - Central",
                HandlerPath,
                140,
                250,
                new Vector2(0f, 11.5f),
                -12),
        };

        [MenuItem("Tools/Rustline/Apply Salvage Intake Macro Dressing")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Macro Environment Kit v0 was applied at native scale with no gameplay colliders.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Macro Dressing")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the graybox first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Macro Environment Kit v0 validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Run Rebuild Salvage Intake Graybox first.");

            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "Sprite-Unlit-Default material is unavailable.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing. Rebuild the graybox first.");

            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Require(artRoot != null,
                "Art Dressing - Preserve is missing. Rebuild the SalvageIntake graybox first.");

            Transform oldMacroRoot = artRoot.Find(MacroRootName);
            if (oldMacroRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldMacroRoot.gameObject);
            }

            GameObject macroRoot = new GameObject(MacroRootName);
            macroRoot.transform.SetParent(artRoot, false);

            foreach (DressingPlacement placement in Placements)
            {
                Sprite sprite = RequireProductionSprite(placement);
                GameObject spriteObject = new GameObject(placement.Name);
                spriteObject.transform.SetParent(macroRoot.transform, false);
                spriteObject.transform.position = new Vector3(placement.Position.x, placement.Position.y, 0f);
                spriteObject.transform.localScale = Vector3.one;

                SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = unlitMaterial;
                renderer.sortingOrder = placement.SortingOrder;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying macro dressing.");
            AssetDatabase.SaveAssets();
            ValidateScene(scene);
        }

        private static Sprite RequireProductionSprite(DressingPlacement placement)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(placement.AssetPath);
            Require(sprite != null, "Missing production sprite: " + placement.AssetPath);
            Require(Mathf.Approximately(sprite.pixelsPerUnit, 16f),
                placement.AssetPath + " must import at 16 PPU.");
            Require(Mathf.RoundToInt(sprite.rect.width) == placement.SourceWidth &&
                Mathf.RoundToInt(sprite.rect.height) == placement.SourceHeight,
                placement.AssetPath + " source dimensions changed unexpectedly.");
            return sprite;
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Macro dressing validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform macroRoot = artRoot?.Find(MacroRootName);
            Require(macroRoot != null, "Macro Environment Kit v0 root is missing.");

            var renderers = new List<SpriteRenderer>(macroRoot.GetComponentsInChildren<SpriteRenderer>(true));
            Require(renderers.Count == Placements.Length,
                "Macro Environment Kit v0 must contain exactly five SpriteRenderers.");
            Require(macroRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Macro environment dressing must remain non-colliding.");

            foreach (DressingPlacement placement in Placements)
            {
                Transform child = macroRoot.Find(placement.Name);
                Require(child != null, "Missing macro dressing object: " + placement.Name);
                Require(child.localScale == Vector3.one,
                    placement.Name + " must remain at native scale 1.0.");
                Require(Mathf.Approximately(child.position.x, placement.Position.x) &&
                    Mathf.Approximately(child.position.y, placement.Position.y),
                    placement.Name + " moved away from the current macro composition contract.");

                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                Sprite expected = RequireProductionSprite(placement);
                Require(renderer != null && renderer.sprite == expected &&
                    renderer.sortingOrder == placement.SortingOrder,
                    placement.Name + " renderer contract is invalid.");
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
