using System;
using System.Collections.Generic;
using System.Linq;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Physics;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Builds the first production-area graybox without mutating the accepted MovementLab.
    ///
    /// The builder owns only the explicitly named Managed roots. Art Dressing and Gameplay Content
    /// roots are preserved across rebuilds so the room can gradually transition from graybox to a
    /// hand-dressed production slice without making the deterministic geometry tool destructive.
    /// </summary>
    public static class RustlineSalvageIntakeSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string MovementLabPath = "Assets/Scenes/MovementLab.unity";
        private const string ArtShowcasePath = "Assets/Scenes/ArtShowcase.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string CollisionTilePath =
            "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string PenumbraShaderPath = "Assets/Shaders/RustlinePalettePenumbra.shader";
        private const string PresentationShaderPath = "Assets/Shaders/RustlineNativePixelPresent.shader";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string EnvironmentManagedName = "Environment - Managed Graybox";
        private const string PlayerRigManagedName = "Player Rig - Managed";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string GameplayContentPreserveName = "Gameplay Content - Preserve";
        private const string GridName = "Environment Grid - 1x1 Cells";
        private const string BackgroundTilemapName = "Background Structure - Visual";
        private const string StructureTilemapName = "Industrial Surface - Visual";
        private const string CollisionTilemapName = "Ground Collision - Hidden";
        private const string PlayerName = "Player - Salvager";
        private const string PlayerSpawnName = "Player Spawn";
        private const string ExitStagingName = "Exit Staging";

        // The first graybox is intentionally tile-aligned. The 24 px / 1.5 u Minimum Traversable
        // Gap remains the authoring floor, while ordinary authored gaps stay at 2+ whole tiles.
        private static readonly CellRect[] StructureRects =
        {
            // Continuous safety floor and room boundary bulkheads.
            new CellRect(-28, -4, 56, 4),
            new CellRect(-28, 0, 2, 16),
            new CellRect(26, 0, 2, 16),

            // West-side service rise: readable 1-2 tile height changes instead of test-course gaps.
            new CellRect(-18, 0, 5, 1),
            new CellRect(-13, 0, 5, 2),
            new CellRect(-8, 0, 5, 4),

            // Central salvage machinery plinth. This is gameplay collision; the eventual hero
            // machinery silhouette will extend far beyond it as non-colliding background art.
            new CellRect(-3, 0, 7, 6),

            // Upper transfer catwalk and its east support.
            new CellRect(4, 5, 11, 1),
            new CellRect(14, 0, 2, 5),

            // East-side low cover / staging geometry before the future exit route.
            new CellRect(18, 0, 4, 2),
        };

        // Background-only masses establish the composition and reserve visual space for the hero
        // transfer machine. They deliberately have no Collider2D and are not traversal authority.
        private static readonly CellRect[] BackgroundRects =
        {
            new CellRect(-26, 12, 52, 1),
            new CellRect(-22, 3, 2, 8),
            new CellRect(20, 3, 2, 8),
            new CellRect(-8, 4, 16, 7),
            new CellRect(-5, 11, 10, 1),
            new CellRect(-12, 8, 4, 1),
            new CellRect(8, 7, 5, 1),
        };

        private static readonly Vector3 PlayerSpawnPosition = new Vector3(-24f, 0.08f, 0f);
        private static readonly Vector3 ExitStagingPosition = new Vector3(24f, 0.08f, 0f);

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

        [MenuItem("Tools/Rustline/Rebuild Salvage Intake Graybox")]
        public static void RebuildFromMenu()
        {
            BuildAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "SalvageIntake was rebuilt and validated. Art Dressing and Gameplay Content were preserved.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Graybox")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake has not been generated yet. Run Rebuild Salvage Intake Graybox first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog("Rustline Salvage Intake", "SalvageIntake validation passed.", "OK");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildAndValidate();
                Debug.Log("RUSTLINE_SALVAGE_INTAKE_VALIDATION_OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                throw;
            }
        }

        private static void BuildAndValidate()
        {
            // Validate the frozen movement/player foundation before building content on top of it.
            RustlineM1ASetup.ValidateAllOrThrow(reopenMovementLabFromDisk: true);

            EnsureFolder("Assets/Scenes/Demo");
            Require(LayerMask.NameToLayer("Ground") == 6,
                "SalvageIntake requires the accepted Ground layer at index 6.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(ruleTile != null && collisionTile != null && playerPrefab != null && unlitMaterial != null,
                "SalvageIntake dependencies are missing. Rebuild/validate the accepted M0/M1 foundation first.");

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            scene.name = "SalvageIntake";

            GameObject root = FindGameObject(scene, RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
            }

            EnsurePreservedChild(root.transform, ArtDressingPreserveName);
            EnsurePreservedChild(root.transform, GameplayContentPreserveName);
            DestroyDirectChildIfPresent(root.transform, EnvironmentManagedName);
            DestroyDirectChildIfPresent(root.transform, PlayerRigManagedName);

            CreateEnvironment(root.transform, ruleTile, collisionTile, unlitMaterial);
            CreatePlayerRig(root.transform, playerPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath), "Could not save SalvageIntake scene.");
            PutSceneAfterLabsInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Re-open from disk so validation inspects the serialized scene rather than only the
            // just-mutated in-memory Tilemap state.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
        }

        private static void CreateEnvironment(
            Transform root,
            RuleTile ruleTile,
            Tile collisionTile,
            Material unlitMaterial)
        {
            GameObject environment = new GameObject(EnvironmentManagedName);
            environment.transform.SetParent(root, false);

            GameObject gridObject = new GameObject(GridName);
            gridObject.transform.SetParent(environment.transform, false);
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            Tilemap background = CreateTilemap(
                gridObject.transform, BackgroundTilemapName, -20, unlitMaterial);
            Tilemap structure = CreateTilemap(
                gridObject.transform, StructureTilemapName, 0, unlitMaterial);
            Tilemap collision = CreateTilemap(
                gridObject.transform, CollisionTilemapName, -1, unlitMaterial);
            collision.gameObject.layer = 6;
            collision.GetComponent<TilemapRenderer>().enabled = false;

            Rigidbody2D terrainBody = collision.gameObject.AddComponent<Rigidbody2D>();
            terrainBody.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tilemapCollider = collision.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = collision.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            TilemapCompositeColliderInitializer2D initializer =
                collision.gameObject.AddComponent<TilemapCompositeColliderInitializer2D>();

            Vector3Int[] structureCells = CollectCells(StructureRects);
            Vector3Int[] backgroundCells = CollectCells(BackgroundRects);
            SetTiles(background, backgroundCells, ruleTile);
            SetTiles(structure, structureCells, ruleTile);
            SetTiles(collision, structureCells, collisionTile);

            // Preserve the release-critical immediate generation sequence used by MovementLab.
            collision.RefreshAllTiles();
            initializer.EnsureGeometry();
            Require(composite.pathCount > 0 && composite.pointCount > 0,
                "SalvageIntake Composite collision geometry is empty after deterministic bake.");

            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(structure);
            EditorUtility.SetDirty(collision);
            EditorUtility.SetDirty(tilemapCollider);
            EditorUtility.SetDirty(composite);
        }

        private static void CreatePlayerRig(Transform root, GameObject playerPrefab)
        {
            Scene scene = root.gameObject.scene;
            GameObject rig = new GameObject(PlayerRigManagedName);
            rig.transform.SetParent(root, false);

            GameObject spawn = new GameObject(PlayerSpawnName);
            spawn.transform.SetParent(rig.transform, false);
            spawn.transform.position = PlayerSpawnPosition;

            GameObject exit = new GameObject(ExitStagingName);
            exit.transform.SetParent(rig.transform, false);
            exit.transform.position = ExitStagingPosition;

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            Require(player != null, "Could not instantiate the accepted Player prefab.");
            player.name = PlayerName;
            player.transform.SetParent(rig.transform, true);
            player.transform.position = spawn.transform.position;

            CreateCameras(rig.transform, player.transform);

            NativePixelPresentation presentation = FindInScene<NativePixelPresentation>(scene);
            PlayerAim2D playerAim = player.GetComponent<PlayerAim2D>();
            PlayerWeaponController2D weaponController = player.GetComponent<PlayerWeaponController2D>();
            Require(presentation != null && playerAim != null && weaponController != null,
                "SalvageIntake player/native-pixel dependencies are incomplete.");
            SetObjectReference(playerAim, "nativePixelPresentation", presentation);

            PixelCameraFollow2D cameraFollow = presentation.WorldCamera.GetComponent<PixelCameraFollow2D>();
            LongwatchCameraImpulse2D cameraImpulse =
                presentation.WorldCamera.gameObject.AddComponent<LongwatchCameraImpulse2D>();
            SetObjectReference(cameraImpulse, "weaponController", weaponController);
            SetObjectReference(cameraImpulse, "cameraFollow", cameraFollow);
        }

        private static void CreateCameras(Transform parent, Transform target)
        {
            Shader penumbraShader = AssetDatabase.LoadAssetAtPath<Shader>(PenumbraShaderPath);
            Shader presentationShader = AssetDatabase.LoadAssetAtPath<Shader>(PresentationShaderPath);
            Require(penumbraShader != null && presentationShader != null,
                "SalvageIntake native-pixel shaders are missing.");

            GameObject worldCameraObject = new GameObject("World Camera - Native Pixel Follow");
            worldCameraObject.transform.SetParent(parent, false);
            worldCameraObject.transform.position = new Vector3(target.position.x, target.position.y + 2f, -10f);
            worldCameraObject.tag = "MainCamera";

            Camera worldCamera = worldCameraObject.AddComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 13.125f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = RustlinePalette.DeepSpaceLinear;
            worldCamera.allowHDR = false;
            worldCamera.allowMSAA = false;
            worldCamera.cullingMask = ~0;
            worldCamera.depth = 0f;
            worldCameraObject.AddComponent<UniversalAdditionalCameraData>();

            PixelCameraFollow2D follow = worldCameraObject.AddComponent<PixelCameraFollow2D>();
            SetObjectReference(follow, "target", target);

            GameObject driverCameraObject = new GameObject("Native Pixel Driver Camera");
            driverCameraObject.transform.SetParent(parent, false);
            driverCameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera driverCamera = driverCameraObject.AddComponent<Camera>();
            driverCamera.orthographic = true;
            driverCamera.orthographicSize = 1f;
            driverCamera.clearFlags = CameraClearFlags.Nothing;
            driverCamera.backgroundColor = RustlinePalette.DeepSpaceLinear;
            driverCamera.allowHDR = false;
            driverCamera.allowMSAA = false;
            driverCamera.cullingMask = 0;
            driverCamera.depth = 5f;
            driverCamera.enabled = false;
            driverCameraObject.AddComponent<UniversalAdditionalCameraData>();

            NativePixelPresentation presentation =
                driverCameraObject.AddComponent<NativePixelPresentation>();
            SetObjectReference(presentation, "worldCamera", worldCamera);
            SetObjectReference(presentation, "processingCamera", driverCamera);
            SetObjectReference(presentation, "playerTarget", target);
            SetObjectReference(presentation, "penumbraShader", penumbraShader);
            SetObjectReference(presentation, "presentationShader", presentationShader);
            SetBoolean(presentation, "penumbraEnabled", true);
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "SalvageIntake validation requires the saved production scene.");

            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Require(root.transform.Find(ArtDressingPreserveName) != null &&
                root.transform.Find(GameplayContentPreserveName) != null,
                "SalvageIntake preserved authoring roots are missing.");

            Grid grid = FindInScene<Grid>(scene);
            Require(grid != null && grid.cellSize == Vector3.one,
                "SalvageIntake environment grid must remain 1x1 Unity units / 16x16 source pixels.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Tilemap background = FindTilemap(scene, BackgroundTilemapName);
            Tilemap structure = FindTilemap(scene, StructureTilemapName);
            Tilemap collision = FindTilemap(scene, CollisionTilemapName);
            Require(ruleTile != null && collisionTile != null && unlitMaterial != null &&
                background != null && structure != null && collision != null,
                "SalvageIntake Tilemap dependencies are incomplete.");

            Vector3Int[] structureCells = CollectCells(StructureRects);
            Vector3Int[] backgroundCells = CollectCells(BackgroundRects);
            Require(CountOccupiedCells(structure) == structureCells.Length &&
                CountOccupiedCells(collision) == structureCells.Length &&
                CountOccupiedCells(background) == backgroundCells.Length,
                "SalvageIntake serialized Tilemap cell counts differ from the managed graybox contract.");
            foreach (Vector3Int cell in structureCells)
            {
                Require(structure.GetTile(cell) == ruleTile && collision.GetTile(cell) == collisionTile,
                    "SalvageIntake visual/collision graybox mismatch at " + cell + ".");
            }
            foreach (Vector3Int cell in backgroundCells)
            {
                Require(background.GetTile(cell) == ruleTile,
                    "SalvageIntake background composition is missing its structural tile at " + cell + ".");
            }

            TilemapRenderer backgroundRenderer = background.GetComponent<TilemapRenderer>();
            TilemapRenderer structureRenderer = structure.GetComponent<TilemapRenderer>();
            TilemapRenderer collisionRenderer = collision.GetComponent<TilemapRenderer>();
            Require(backgroundRenderer != null && backgroundRenderer.enabled &&
                backgroundRenderer.sharedMaterial == unlitMaterial && backgroundRenderer.sortingOrder == -20 &&
                structureRenderer != null && structureRenderer.enabled &&
                structureRenderer.sharedMaterial == unlitMaterial && structureRenderer.sortingOrder == 0 &&
                collisionRenderer != null && !collisionRenderer.enabled,
                "SalvageIntake visual/collision renderer contract is invalid.");

            TilemapCollider2D tilemapCollider = collision.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = collision.GetComponent<CompositeCollider2D>();
            Rigidbody2D terrainBody = collision.GetComponent<Rigidbody2D>();
            TilemapCompositeColliderInitializer2D initializer =
                collision.GetComponent<TilemapCompositeColliderInitializer2D>();
            Require(collision.gameObject.layer == 6 && tilemapCollider != null && tilemapCollider.enabled &&
                tilemapCollider.compositeOperation == Collider2D.CompositeOperation.Merge &&
                composite != null && composite.enabled &&
                composite.geometryType == CompositeCollider2D.GeometryType.Polygons &&
                terrainBody != null && terrainBody.bodyType == RigidbodyType2D.Static &&
                initializer != null && initializer.enabled,
                "SalvageIntake must retain the release-hardened hidden Tilemap -> Composite collision contract.");
            collision.RefreshAllTiles();
            initializer.EnsureGeometry();
            Require(composite.pathCount > 0 && composite.pointCount > 0,
                "SalvageIntake Composite geometry is empty after reopening the serialized scene.");

            GameObject player = FindGameObject(scene, PlayerName);
            GameObject spawn = FindGameObject(scene, PlayerSpawnName);
            GameObject exit = FindGameObject(scene, ExitStagingName);
            Require(player != null && spawn != null && exit != null &&
                player.transform.position == PlayerSpawnPosition &&
                spawn.transform.position == PlayerSpawnPosition &&
                exit.transform.position == ExitStagingPosition,
                "SalvageIntake player/spawn/exit staging markers are invalid.");

            NativePixelPresentation presentation = FindInScene<NativePixelPresentation>(scene);
            PlayerAim2D playerAim = player.GetComponent<PlayerAim2D>();
            PlayerWeaponController2D weaponController = player.GetComponent<PlayerWeaponController2D>();
            Require(presentation != null && playerAim != null && weaponController != null &&
                playerAim.NativePixelPresentation == presentation &&
                presentation.PlayerTarget == player.transform && presentation.PenumbraEnabled,
                "SalvageIntake must reuse the accepted native-pixel/penumbra player wiring.");
            Camera worldCamera = presentation.WorldCamera;
            Camera driverCamera = presentation.ProcessingCamera;
            PixelCameraFollow2D cameraFollow = worldCamera?.GetComponent<PixelCameraFollow2D>();
            LongwatchCameraImpulse2D cameraImpulse = worldCamera?.GetComponent<LongwatchCameraImpulse2D>();
            Require(GetComponentsInScene<Camera>(scene).Count == 2 &&
                worldCamera != null && worldCamera.CompareTag("MainCamera") && worldCamera.orthographic &&
                driverCamera != null && driverCamera.cullingMask == 0 && !driverCamera.enabled &&
                cameraFollow != null && cameraImpulse != null &&
                cameraImpulse.CameraFollow == cameraFollow && cameraImpulse.WeaponController == weaponController,
                "SalvageIntake camera/native-pixel/Longwatch impulse rig is incomplete.");
            Require(GetComponentsInScene<Light2D>(scene).Count == 0,
                "SalvageIntake graybox must not introduce identity or decorative Light2D objects.");
        }

        private static Tilemap CreateTilemap(
            Transform parent,
            string name,
            int sortingOrder,
            Material material)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void SetTiles(Tilemap tilemap, Vector3Int[] cells, TileBase tile)
        {
            TileBase[] tiles = Enumerable.Repeat(tile, cells.Length).ToArray();
            tilemap.SetTiles(cells, tiles);
            tilemap.RefreshAllTiles();
        }

        private static Vector3Int[] CollectCells(IEnumerable<CellRect> rects)
        {
            var cells = new HashSet<Vector3Int>();
            foreach (CellRect rect in rects)
            {
                Require(rect.Width > 0 && rect.Height > 0, "SalvageIntake CellRect dimensions must be positive.");
                for (int x = rect.Left; x < rect.Left + rect.Width; x++)
                {
                    for (int y = rect.Bottom; y < rect.Bottom + rect.Height; y++)
                    {
                        cells.Add(new Vector3Int(x, y, 0));
                    }
                }
            }

            return cells.OrderBy(cell => cell.y).ThenBy(cell => cell.x).ToArray();
        }

        private static int CountOccupiedCells(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            return tilemap.GetTilesBlock(bounds).Count(tile => tile != null);
        }

        private static void PutSceneAfterLabsInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != ScenePath)
                .ToList();
            Require(scenes.Count >= 2 && scenes[0].path == MovementLabPath &&
                scenes[1].path == ArtShowcasePath,
                "Accepted MovementLab/ArtShowcase build-order contract changed before SalvageIntake insertion.");
            scenes.Insert(2, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsurePreservedChild(Transform parent, string name)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
        }

        private static void DestroyDirectChildIfPresent(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
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

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static List<T> GetComponentsInScene<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(UnityEngine.Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
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
