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
    /// Applies the first production floor-edge skin to the long lower-bay floor in SalvageIntake.
    /// The authored sprites are visual-only; hidden Tilemap collision remains authoritative.
    ///
    /// This pass also clears only the gray visual Tilemap cells directly replaced by the skin so
    /// transparent pixels reveal the scene/background rather than the temporary graybox surface.
    /// Re-run this command after rebuilding the graybox while this pass remains scene-specific.
    /// </summary>
    public static class RustlineSalvageIntakeFloorDressingSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Structural Floor Skin v0 - Managed";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string FloorLeftPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_left.png";
        private const string FloorMidAPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_mid_a.png";
        private const string FloorMidBPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_mid_b.png";
        private const string FloorRightPath =
            "Assets/Art/Environment/Architecture/Floor/floor_edge_right.png";

        private const int SourceWidth = 48;
        private const int SourceHeight = 32;
        private const int SortingOrder = 2;

        // The six 3 u modules span x=1..19 exactly. This intentionally extends 1 u beneath the
        // central plinth footprint so the long lower-bay run terminates cleanly at the east staging
        // edge without fractional scaling or non-pixel-aligned overlap.
        //
        // Mid A is the common language. Mid B carries stronger rust and appears only once in this
        // first six-piece run. One A is mirrored to get extra variation from the same source art.
        private static readonly FloorPlacement[] Placements =
        {
            new FloorPlacement("Floor Edge - Lower Bay Left", FloorLeftPath, new Vector2(2.5f, -1f)),
            new FloorPlacement("Floor Edge - Lower Bay Mid A 01", FloorMidAPath, new Vector2(5.5f, -1f)),
            new FloorPlacement("Floor Edge - Lower Bay Mid A 02 Mirrored", FloorMidAPath, new Vector2(8.5f, -1f), flipX: true),
            new FloorPlacement("Floor Edge - Lower Bay Mid B Rust", FloorMidBPath, new Vector2(11.5f, -1f)),
            new FloorPlacement("Floor Edge - Lower Bay Mid A 03", FloorMidAPath, new Vector2(14.5f, -1f)),
            new FloorPlacement("Floor Edge - Lower Bay Right", FloorRightPath, new Vector2(17.5f, -1f)),
        };

        private readonly struct FloorPlacement
        {
            internal FloorPlacement(string name, string assetPath, Vector2 position, bool flipX = false)
            {
                Name = name;
                AssetPath = assetPath;
                Position = position;
                FlipX = flipX;
            }

            internal string Name { get; }
            internal string AssetPath { get; }
            internal Vector2 Position { get; }
            internal bool FlipX { get; }
        }

        [MenuItem("Tools/Rustline/Apply Salvage Intake Floor Dressing")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Lower-bay floor dressing was applied at native scale. Hidden gameplay collision was preserved.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Floor Dressing")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist. Rebuild the graybox first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Lower-bay floor dressing validation passed.",
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

            // Remove only the two gray visual rows replaced by the 32 px-tall floor-edge skin.
            // Collision remains untouched and is explicitly checked before visual cells are cleared.
            foreach (Vector3Int cell in EnumerateReplacedVisualCells())
            {
                Require(collision.HasTile(cell),
                    "Expected hidden floor collision is missing at " + cell + ".");
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

            foreach (FloorPlacement placement in Placements)
            {
                Sprite sprite = RequireProductionSprite(placement.AssetPath);
                GameObject spriteObject = new GameObject(placement.Name);
                spriteObject.transform.SetParent(managedRoot.transform, false);
                spriteObject.transform.position = new Vector3(placement.Position.x, placement.Position.y, 0f);
                spriteObject.transform.localScale = Vector3.one;

                SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = unlitMaterial;
                renderer.sortingOrder = SortingOrder;
                renderer.flipX = placement.FlipX;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying floor dressing.");
            AssetDatabase.SaveAssets();
            ValidateScene(scene);
        }

        private static Sprite RequireProductionSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Require(sprite != null, "Missing production sprite: " + assetPath);
            Require(Mathf.Approximately(sprite.pixelsPerUnit, 16f),
                assetPath + " must import at 16 PPU.");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for production sprite: " + assetPath);
            Require(importer.filterMode == FilterMode.Point,
                assetPath + " must use Point filtering.");
            Require(!importer.mipmapEnabled,
                assetPath + " must keep mipmaps disabled.");

            // Unity 6000.4 serializes spriteGenerateFallbackPhysicsShape in the .meta file but does
            // not expose it as a public TextureImporter property. Do not bind editor validation to
            // that unavailable API. This dressing never creates SpriteCollider2D components, and
            // ValidateScene separately enforces that the managed floor-skin root contains no Collider2D.

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth == SourceWidth && sourceHeight == SourceHeight,
                assetPath + " source dimensions changed unexpectedly. Expected " +
                SourceWidth + "x" + SourceHeight + ", got " + sourceWidth + "x" + sourceHeight + ".");

            return sprite;
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Floor dressing validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Structural Floor Skin v0 managed root is missing.");

            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(structure != null && collision != null,
                "SalvageIntake visual/collision Tilemaps are missing.");

            foreach (Vector3Int cell in EnumerateReplacedVisualCells())
            {
                Require(!structure.HasTile(cell),
                    "Gray visual floor tile was restored beneath production floor skin at " + cell + ".");
                Require(collision.HasTile(cell),
                    "Production floor skin must not remove hidden collision at " + cell + ".");
            }

            var renderers = new List<SpriteRenderer>(managedRoot.GetComponentsInChildren<SpriteRenderer>(true));
            Require(renderers.Count == Placements.Length,
                "Structural Floor Skin v0 must contain exactly the configured SpriteRenderers.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Floor dressing must remain non-colliding.");

            foreach (FloorPlacement placement in Placements)
            {
                Transform child = managedRoot.Find(placement.Name);
                Require(child != null, "Missing floor dressing object: " + placement.Name);
                Require(child.localScale == Vector3.one,
                    placement.Name + " must remain at native scale 1.0.");
                Require(Mathf.Approximately(child.position.x, placement.Position.x) &&
                    Mathf.Approximately(child.position.y, placement.Position.y),
                    placement.Name + " moved away from the current floor composition contract.");

                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                Sprite expected = RequireProductionSprite(placement.AssetPath);
                Require(renderer != null && renderer.sprite == expected &&
                    renderer.sortingOrder == SortingOrder && renderer.flipX == placement.FlipX,
                    placement.Name + " renderer contract is invalid.");
            }
        }

        private static IEnumerable<Vector3Int> EnumerateReplacedVisualCells()
        {
            for (int x = 1; x < 19; x++)
            {
                for (int y = -2; y < 0; y++)
                {
                    yield return new Vector3Int(x, y, 0);
                }
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
