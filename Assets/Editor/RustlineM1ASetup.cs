using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rustline.Diagnostics;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Builds the deliberately small M1A movement prefab and diagnostic lab.
    /// It is safe to rerun; tuning assets retain their values once created.
    /// </summary>
    public static class RustlineM1ASetup
    {
        private const string ScenePath = "Assets/Scenes/MovementLab.unity";
        private const string ArtShowcasePath = "Assets/Scenes/ArtShowcase.unity";
        private const string ConfigPath = "Assets/Config/Player/PlayerMovementConfig.asset";
        private const string PhysicsMaterialPath = "Assets/Config/Player/PlayerFrictionless.physicsMaterial2D";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string ControllerPath = "Assets/Art/Characters/Player/Animations/PlayerGameplay.controller";
        private const string CollisionTilePath = "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";
        private const string RuleTilePath = "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private const string BaseSpritePath = "Assets/Art/Characters/Player/player_salvager_base_right.png";
        private const string PenumbraShaderPath = "Assets/Shaders/RustlinePalettePenumbra.shader";
        private const string PresentationShaderPath = "Assets/Shaders/RustlineNativePixelPresent.shader";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string PenumbraLayerName = "RustlinePenumbra";
        private const string PresentationLayerName = "RustlinePresentation";
        private const int PenumbraLayerIndex = 28;
        private const int PresentationLayerIndex = 29;

        private static readonly CourseBlock[] Course =
        {
            new CourseBlock(-30, 0, 16, 4),
            new CourseBlock(-11, 1, 6, 4),
            new CourseBlock(-1, 0, 7, 4),
            new CourseBlock(9, 2, 9, 6),
            new CourseBlock(21, -3, 11, 4),
            new CourseBlock(35, -2, 6, 4),
            new CourseBlock(44, -1, 6, 4),
            new CourseBlock(53, 0, 10, 4),
        };

        private readonly struct CourseBlock
        {
            internal CourseBlock(int left, int top, int width, int depth)
            {
                Left = left;
                Top = top;
                Width = width;
                Depth = depth;
            }

            internal int Left { get; }
            internal int Top { get; }
            internal int Width { get; }
            internal int Depth { get; }
        }

        [MenuItem("Tools/Rustline/Rebuild M1A Movement Lab")]
        public static void RebuildFromMenu()
        {
            BuildAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline M1A Movement Lab",
                "The player prefab, movement assets, and MovementLab scene were rebuilt and validated.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate M1A Movement")]
        public static void ValidateFromMenu()
        {
            ValidateAllOrThrow(reopenMovementLabFromDisk: true);
            EditorUtility.DisplayDialog("Rustline M1A Movement", "All deterministic M1A checks passed.", "OK");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildAndValidate();
                Debug.Log("RUSTLINE_M1A_VALIDATION_OK");
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
            EnsureFolder("Assets/Config/Player");
            EnsureFolder("Assets/Prefabs/Player");
            int groundLayer = EnsureGroundLayer();
            int penumbraLayer = EnsureLayer(PenumbraLayerName, PenumbraLayerIndex);
            int presentationLayer = EnsureLayer(PresentationLayerName, PresentationLayerIndex);

            PlayerMovementConfig config = CreateConfig();
            PhysicsMaterial2D physicsMaterial = CreatePhysicsMaterial();
            AnimatorController controller = CreateGameplayController();
            Tile collisionTile = CreateCollisionTile();
            GameObject prefab = CreatePlayerPrefab(config, physicsMaterial, controller, groundLayer);
            CreateMovementLab(
                config,
                prefab,
                collisionTile,
                groundLayer,
                penumbraLayer,
                presentationLayer);
            PutMovementScenesFirstInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateAllOrThrow(reopenMovementLabFromDisk: true);
        }

        private static PlayerMovementConfig CreateConfig()
        {
            PlayerMovementConfig config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            EditorUtility.SetDirty(config);
            return config;
        }

        private static PhysicsMaterial2D CreatePhysicsMaterial()
        {
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("Player Frictionless");
                AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
            }

            material.friction = 0f;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimatorController CreateGameplayController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(child.stateMachine);
            }

            string[] stateNames = { "Idle", "Run", "Jump", "Fall", "Land" };
            foreach (string stateName in stateNames)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    $"Assets/Art/Characters/Player/Animations/Player_{stateName}.anim");
                Require(clip != null, "Missing accepted M0 animation clip: Player_" + stateName);
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;
                if (stateName == "Idle")
                {
                    stateMachine.defaultState = state;
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Tile CreateCollisionTile()
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = "Movement Collision Tile";
                AssetDatabase.CreateAsset(tile, CollisionTilePath);
            }

            tile.sprite = null;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static GameObject CreatePlayerPrefab(
            PlayerMovementConfig config,
            PhysicsMaterial2D physicsMaterial,
            RuntimeAnimatorController controller,
            int groundLayer)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Require(inputActions != null, "Input action asset is missing: " + InputPath);
            Sprite baseSprite = AssetDatabase.LoadAllAssetsAtPath(BaseSpritePath).OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name.EndsWith("_0", StringComparison.Ordinal));
            Require(baseSprite != null, "Fixed-cell player base sprite is missing.");

            GameObject root = new GameObject("Player");
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sharedMaterial = physicsMaterial;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(1.05f, 2.75f);
            collider.offset = new Vector2(0f, 1.375f);
            collider.sharedMaterial = physicsMaterial;

            PlayerInputReader input = root.AddComponent<PlayerInputReader>();
            SetObjectReference(input, "inputActions", inputActions);

            PlayerGroundProbe2D probe = root.AddComponent<PlayerGroundProbe2D>();
            SetObjectReference(probe, "config", config);
            SetInteger(probe, "groundLayers", 1 << groundLayer);

            PlayerMotor2D motor = root.AddComponent<PlayerMotor2D>();
            SetObjectReference(motor, "config", config);

            GameObject visual = new GameObject("Visual - 48x64 Full Cell");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = baseSprite;
            renderer.sortingOrder = 10;
            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PlayerAnimator2D presentation = root.AddComponent<PlayerAnimator2D>();
            SetObjectReference(presentation, "config", config);
            SetObjectReference(presentation, "animator", animator);
            SetObjectReference(presentation, "spriteRenderer", renderer);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Require(prefab != null, "Failed to create the player prefab.");
            return prefab;
        }

        private static void CreateMovementLab(
            PlayerMovementConfig config,
            GameObject playerPrefab,
            TileBase collisionTile,
            int groundLayer,
            int penumbraLayer,
            int presentationLayer)
        {
            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Require(ruleTile != null, "Accepted M0 IndustrialSurface Rule Tile is missing.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MovementLab";
            GameObject root = new GameObject("RUSTLINE M1A - MOVEMENT LAB");

            CreateGlobalLight(root.transform);
            CreateLabels(root.transform);

            GameObject gridObject = new GameObject("Environment Grid - 1x1 Cells");
            gridObject.transform.SetParent(root.transform, false);
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            Tilemap visualTilemap = CreateTilemap(gridObject.transform, "Industrial Surface - Visual", 0);
            Tilemap collisionTilemap = CreateTilemap(gridObject.transform, "Ground Collision - Hidden", -1);
            collisionTilemap.gameObject.layer = groundLayer;
            collisionTilemap.GetComponent<TilemapRenderer>().enabled = false;

            Rigidbody2D terrainBody = collisionTilemap.gameObject.AddComponent<Rigidbody2D>();
            terrainBody.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D tilemapCollider = collisionTilemap.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = collisionTilemap.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            Vector3Int[] courseCells = GetCourseCells().ToArray();
            TileBase[] visualTiles = Enumerable.Repeat<TileBase>(ruleTile, courseCells.Length).ToArray();
            TileBase[] collisionTiles = Enumerable.Repeat(collisionTile, courseCells.Length).ToArray();
            visualTilemap.SetTiles(courseCells, visualTiles);
            collisionTilemap.SetTiles(courseCells, collisionTiles);
            visualTilemap.RefreshAllTiles();
            collisionTilemap.RefreshAllTiles();

            // Tilemap.SetTiles changes native tile data. The collision Tilemap happens to receive
            // additional dirtiness through TilemapCollider2D; the visual Rule Tile map does not.
            // Explicitly dirty both maps and the scene so their serialized tile arrays survive saving.
            EditorUtility.SetDirty(visualTilemap);
            EditorUtility.SetDirty(visualTilemap.GetComponent<TilemapRenderer>());
            EditorUtility.SetDirty(collisionTilemap);
            EditorUtility.SetDirty(collisionTilemap.GetComponent<TilemapRenderer>());
            EditorSceneManager.MarkSceneDirty(scene);

            GameObject spawn = new GameObject("Player Spawn");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.position = new Vector3(-27f, 0.08f, 0f);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player - Movement Specimen";
            player.transform.position = spawn.transform.position;
            MovementLabRespawn respawn = player.AddComponent<MovementLabRespawn>();
            SetObjectReference(respawn, "spawnPoint", spawn.transform);
            SetFloat(respawn, "failureHeight", -12f);

            CreateCamera(root.transform, player.transform, penumbraLayer, presentationLayer);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void CreateCamera(
            Transform parent,
            Transform target,
            int penumbraLayer,
            int presentationLayer)
        {
            Shader penumbraShader = AssetDatabase.LoadAssetAtPath<Shader>(PenumbraShaderPath);
            Require(penumbraShader != null, "Native pixel penumbra shader is missing: " + PenumbraShaderPath);
            Shader presentationShader = AssetDatabase.LoadAssetAtPath<Shader>(PresentationShaderPath);
            Require(presentationShader != null,
                "Native pixel presentation shader is missing: " + PresentationShaderPath);

            GameObject worldCameraObject = new GameObject("World Camera - Native Pixel Follow");
            worldCameraObject.transform.SetParent(parent, false);
            worldCameraObject.transform.position = new Vector3(target.position.x, target.position.y + 2f, -10f);
            worldCameraObject.tag = "MainCamera";

            Camera worldCamera = worldCameraObject.AddComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 13.125f; // 420 logical px at 16 PPU; runtime follows the window.
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = RustlinePalette.DeepSpaceLinear;
            worldCamera.allowHDR = false;
            worldCamera.allowMSAA = false;
            worldCamera.cullingMask = ~((1 << penumbraLayer) | (1 << presentationLayer));
            worldCamera.depth = 0f;
            worldCameraObject.AddComponent<UniversalAdditionalCameraData>();

            PixelCameraFollow2D follow = worldCameraObject.AddComponent<PixelCameraFollow2D>();
            SetObjectReference(follow, "target", target);

            GameObject processingCameraObject = new GameObject("Penumbra Camera - Logical Pass");
            processingCameraObject.transform.SetParent(parent, false);
            processingCameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera processingCamera = processingCameraObject.AddComponent<Camera>();
            processingCamera.orthographic = true;
            processingCamera.orthographicSize = 1f;
            processingCamera.clearFlags = CameraClearFlags.SolidColor;
            processingCamera.backgroundColor = RustlinePalette.DeepSpaceLinear;
            processingCamera.allowHDR = false;
            processingCamera.allowMSAA = false;
            processingCamera.cullingMask = 1 << penumbraLayer;
            processingCamera.depth = 5f;
            processingCamera.enabled = false;
            processingCameraObject.AddComponent<UniversalAdditionalCameraData>();

            MeshRenderer processingRenderer = CreatePresentationQuad(
                parent,
                "Logical Penumbra Pass Quad",
                penumbraLayer);

            GameObject presentationCameraObject = new GameObject("Presentation Camera - Deep Space Surround");
            presentationCameraObject.transform.SetParent(parent, false);
            presentationCameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera presentationCamera = presentationCameraObject.AddComponent<Camera>();
            presentationCamera.orthographic = true;
            presentationCamera.orthographicSize = 1f;
            presentationCamera.clearFlags = CameraClearFlags.SolidColor;
            presentationCamera.backgroundColor = RustlinePalette.DeepSpaceLinear;
            presentationCamera.allowHDR = false;
            presentationCamera.allowMSAA = false;
            presentationCamera.cullingMask = 1 << presentationLayer;
            presentationCamera.depth = 10f;
            presentationCamera.enabled = false;
            presentationCameraObject.AddComponent<UniversalAdditionalCameraData>();

            MeshRenderer presentationRenderer = CreatePresentationQuad(
                parent,
                "Physical Native Pixel Presentation Quad",
                presentationLayer);

            NativePixelPresentation presentation = presentationCameraObject.AddComponent<NativePixelPresentation>();
            SetObjectReference(presentation, "worldCamera", worldCamera);
            SetObjectReference(presentation, "processingCamera", processingCamera);
            SetObjectReference(presentation, "presentationCamera", presentationCamera);
            SetObjectReference(presentation, "playerTarget", target);
            SetObjectReference(presentation, "processingRenderer", processingRenderer);
            SetObjectReference(presentation, "presentationRenderer", presentationRenderer);
            SetObjectReference(presentation, "penumbraShader", penumbraShader);
            SetObjectReference(presentation, "presentationShader", presentationShader);
            SetBoolean(presentation, "penumbraEnabled", true);
        }

        private static MeshRenderer CreatePresentationQuad(
            Transform parent,
            string name,
            int layer)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.layer = layer;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = null;
            renderer.enabled = false;
            return renderer;
        }

        private static void CreateGlobalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Global Light 2D");
            lightObject.transform.SetParent(parent, false);
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        private static void CreateLabels(Transform parent)
        {
            GameObject labels = new GameObject("Diagnostic Labels");
            labels.transform.SetParent(parent, false);
            Color cyan = new Color32(32, 237, 229, 255);
            Color neutral = new Color32(201, 187, 177, 255);
            Color warning = new Color32(253, 208, 69, 255);

            CreateLabel(labels.transform, "M1A MOVEMENT LAB", new Vector3(-27f, 6.5f, -0.2f), 0.19f, cyan);
            CreateLabel(labels.transform, "A/D or ARROWS  |  SPACE  |  GAMEPAD STICK/DPAD + SOUTH BUTTON",
                new Vector3(-27f, 5.9f, -0.2f), 0.095f, neutral);
            CreateLabel(labels.transform, "ACCEL / DECEL / REVERSAL", new Vector3(-22.5f, 2f, -0.2f), 0.09f, neutral);
            CreateLabel(labels.transform, "COYOTE GAPS", new Vector3(-8f, 3.2f, -0.2f), 0.09f, warning);
            CreateLabel(labels.transform, "JUMP UP + VARIABLE HEIGHT", new Vector3(13.5f, 4.2f, -0.2f), 0.09f, neutral);
            CreateLabel(labels.transform, "DROP + BUFFER BEFORE LANDING", new Vector3(26.5f, 0f, -0.2f), 0.09f, warning);
            CreateLabel(labels.transform, "STEP COURSE / AIR CONTROL", new Vector3(47.5f, 2.2f, -0.2f), 0.09f, neutral);
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position, float size, Color color)
        {
            GameObject labelObject = new GameObject(text);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = size;
            textMesh.fontSize = 32;
            textMesh.color = color;
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = textMesh.font.material;
            renderer.sortingOrder = 100;
        }

        private static void PutMovementScenesFirstInBuildSettings()
        {
            List<EditorBuildSettingsScene> remaining = EditorBuildSettings.scenes
                .Where(scene => scene.path != ScenePath && scene.path != ArtShowcasePath)
                .ToList();
            remaining.Insert(0, new EditorBuildSettingsScene(ArtShowcasePath, true));
            remaining.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = remaining.ToArray();
        }

        internal static void ValidateAllOrThrow(bool reopenMovementLabFromDisk = true)
        {
            PlayerMovementConfig config = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(ConfigPath);
            Require(config != null, "Player movement config is missing.");
            Require(config.IsSane(out string configReason), "Player movement config is invalid: " + configReason);

            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputActionMap playerMap = input?.FindActionMap("Player", false);
            Require(input != null && input.actionMaps.Count == 1 && playerMap != null,
                "Input asset must contain only the focused Player action map for M1A.");
            Require(playerMap.actions.Count == 2 && playerMap.FindAction("Move", false) != null &&
                playerMap.FindAction("Jump", false) != null, "Player input must contain only Move and Jump.");
            Require(playerMap.FindAction("Move").bindings.Any(binding => binding.path == "<Gamepad>/dpad"),
                "Move must support the gamepad D-pad.");
            Require(playerMap.FindAction("Jump").bindings.Any(binding => binding.path == "<Keyboard>/space") &&
                playerMap.FindAction("Jump").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"),
                "Jump bindings are incomplete.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Require(prefab != null, "Player prefab is missing.");
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = prefab.GetComponent<CapsuleCollider2D>();
            Require(body != null && body.bodyType == RigidbodyType2D.Dynamic && Mathf.Approximately(body.gravityScale, 0f),
                "Player must use a dynamic Rigidbody2D with motor-controlled gravity.");
            Require(collider != null && collider.direction == CapsuleDirection2D.Vertical &&
                collider.size == new Vector2(1.05f, 2.75f) && collider.offset == new Vector2(0f, 1.375f),
                "Player collision shape changed from the stable full-cell-relative contract.");
            Require(prefab.GetComponent<PlayerInputReader>() != null && prefab.GetComponent<PlayerGroundProbe2D>() != null &&
                prefab.GetComponent<PlayerMotor2D>() != null && prefab.GetComponent<PlayerAnimator2D>() != null,
                "Player prefab movement components are incomplete.");
            Transform visual = prefab.transform.Find("Visual - 48x64 Full Cell");
            Require(visual != null && Vector3.Distance(visual.localPosition, new Vector3(0f, -0.25f, 0f)) < 0.0001f,
                "Player visual child must be lowered exactly 4 source pixels (-0.25 Unity units).");
            Require(prefab.GetComponentInChildren<SpriteRenderer>()?.transform.localScale == Vector3.one,
                "Player art must render at integer 1x scale.");

            Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            Require(collisionTile != null && collisionTile.sprite == null && collisionTile.colliderType == Tile.ColliderType.Grid,
                "Hidden collision tile must use an unsmoothed grid collider and no sprite.");
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null, "MovementLab scene is missing.");
            Require(EditorBuildSettings.scenes.Length >= 2 && EditorBuildSettings.scenes[0].path == ScenePath &&
                EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[1].path == ArtShowcasePath &&
                EditorBuildSettings.scenes[1].enabled, "MovementLab and ArtShowcase must lead the build settings.");

            ValidateScene(reopenMovementLabFromDisk);
            RustlineM0ArtSetup.ValidateAllOrThrow();

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string manifest = File.ReadAllText(Path.Combine(projectRoot, "Packages", "manifest.json"));
            string packageLock = File.ReadAllText(Path.Combine(projectRoot, "Packages", "packages-lock.json"));
            string packageText = manifest + packageLock;
            Require(packageText.IndexOf("multiplayer", StringComparison.OrdinalIgnoreCase) < 0 &&
                packageText.IndexOf("netcode", StringComparison.OrdinalIgnoreCase) < 0,
                "Networking or multiplayer packages are present.");
        }

        private static void ValidateScene(bool reopenFromDisk)
        {
            Scene scene;
            bool openedForValidation;
            if (reopenFromDisk)
            {
                // Opening the saved path after replacing the active scene ensures validation reads
                // the YAML/asset database result, not unsaved in-memory Tilemap state.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                openedForValidation = false;
            }
            else
            {
                scene = SceneManager.GetSceneByPath(ScenePath);
                openedForValidation = !scene.IsValid() || !scene.isLoaded;
                if (openedForValidation)
                {
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                }
            }

            try
            {
                Grid grid = FindInScene<Grid>(scene);
                Require(grid != null && grid.cellSize == Vector3.one, "MovementLab grid must use 1x1 cells.");
                RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
                Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
                Require(ruleTile != null && collisionTile != null, "MovementLab Tilemap assets are missing.");

                Tilemap visualTilemap = FindTilemap(scene, "Industrial Surface - Visual");
                Tilemap collisionTilemap = FindTilemap(scene, "Ground Collision - Hidden");
                Require(visualTilemap != null, "MovementLab visual Tilemap is missing.");
                Require(collisionTilemap != null, "MovementLab collision Tilemap is missing.");
                Require(visualTilemap.GetComponent<TilemapRenderer>()?.enabled == true,
                    "MovementLab visual Tilemap renderer must remain enabled.");
                Require(collisionTilemap.GetComponent<TilemapRenderer>()?.enabled == false,
                    "MovementLab collision Tilemap renderer must remain disabled.");

                Vector3Int[] courseCells = GetCourseCells().ToArray();
                Require(CountOccupiedCells(visualTilemap) == courseCells.Length,
                    "MovementLab visual Tilemap occupied-cell count does not match the course geometry.");
                Require(CountOccupiedCells(collisionTilemap) == courseCells.Length,
                    "MovementLab collision Tilemap occupied-cell count does not match the course geometry.");
                foreach (Vector3Int cell in courseCells)
                {
                    Require(visualTilemap.GetTile(cell) == ruleTile,
                        "MovementLab visual Tilemap must use IndustrialSurfaceRuleTile at " + cell + ".");
                    Require(collisionTilemap.GetTile(cell) == collisionTile,
                        "MovementLab collision Tilemap is missing its collision tile at " + cell + ".");
                }

                TilemapCollider2D collider = FindInScene<TilemapCollider2D>(scene);
                Require(collider != null && collider.compositeOperation == Collider2D.CompositeOperation.Merge &&
                    collider.GetComponent<CompositeCollider2D>() != null,
                    "MovementLab must use composite Tilemap collision.");
                Camera worldCamera = FindCamera(scene, "World Camera - Native Pixel Follow");
                Camera processingCamera = FindCamera(scene, "Penumbra Camera - Logical Pass");
                Camera presentationCamera = FindCamera(scene, "Presentation Camera - Deep Space Surround");
                int penumbraLayer = LayerMask.NameToLayer(PenumbraLayerName);
                int presentationLayer = LayerMask.NameToLayer(PresentationLayerName);
                Require(
                    penumbraLayer == PenumbraLayerIndex && presentationLayer == PresentationLayerIndex,
                    "MovementLab presentation layers are missing or use unexpected indices.");
                Require(worldCamera != null && worldCamera.orthographic && !worldCamera.allowHDR &&
                    !worldCamera.allowMSAA && worldCamera.CompareTag("MainCamera") &&
                    (worldCamera.cullingMask & (1 << penumbraLayer)) == 0 &&
                    (worldCamera.cullingMask & (1 << presentationLayer)) == 0,
                    "MovementLab logical world camera configuration is invalid.");
                Require(worldCamera.GetComponent<PixelCameraFollow2D>() != null,
                    "MovementLab camera follow is missing.");
                Require(processingCamera != null && processingCamera.orthographic &&
                    processingCamera.cullingMask == (1 << penumbraLayer) &&
                    !processingCamera.allowHDR && !processingCamera.allowMSAA &&
                    processingCamera.targetTexture == null && !processingCamera.enabled &&
                    Mathf.Approximately(processingCamera.depth, 5f),
                    "MovementLab logical penumbra camera configuration is invalid.");
                Require(presentationCamera != null && presentationCamera.orthographic &&
                    presentationCamera.cullingMask == (1 << presentationLayer) &&
                    !presentationCamera.allowHDR && !presentationCamera.allowMSAA &&
                    presentationCamera.targetTexture == null && !presentationCamera.enabled &&
                    Mathf.Approximately(presentationCamera.depth, 10f),
                    "MovementLab physical presentation camera configuration is invalid.");

                GameObject processingQuad = FindGameObject(scene, "Logical Penumbra Pass Quad");
                GameObject presentationQuad = FindGameObject(scene, "Physical Native Pixel Presentation Quad");
                MeshRenderer processingRenderer = processingQuad?.GetComponent<MeshRenderer>();
                MeshRenderer presentationRenderer = presentationQuad?.GetComponent<MeshRenderer>();
                Require(processingQuad != null && processingQuad.layer == penumbraLayer &&
                    processingRenderer != null && !processingRenderer.enabled &&
                    processingQuad.GetComponent<MeshCollider>() == null,
                    "MovementLab logical penumbra quad isolation/configuration is invalid.");
                Require(presentationQuad != null && presentationQuad.layer == presentationLayer &&
                    presentationRenderer != null && !presentationRenderer.enabled &&
                    presentationQuad.GetComponent<MeshCollider>() == null,
                    "MovementLab physical presentation quad isolation/configuration is invalid.");

                NativePixelPresentation presentation = FindInScene<NativePixelPresentation>(scene);
                Shader penumbraShader = AssetDatabase.LoadAssetAtPath<Shader>(PenumbraShaderPath);
                Shader presentationShader = AssetDatabase.LoadAssetAtPath<Shader>(PresentationShaderPath);
                Require(presentation != null && presentation.WorldCamera == worldCamera &&
                    presentation.ProcessingCamera == processingCamera &&
                    presentation.PresentationCamera == presentationCamera && presentation.PlayerTarget != null &&
                    presentation.PlayerTarget.name == "Player - Movement Specimen" &&
                    presentation.ProcessingRenderer == processingRenderer &&
                    presentation.PresentationRenderer == presentationRenderer &&
                    presentation.PenumbraShader == penumbraShader &&
                    presentation.PresentationShader == presentationShader &&
                    presentation.PenumbraEnabled,
                    "MovementLab native pixel presentation references/defaults are invalid.");
                Require(penumbraShader != null && !ShaderUtil.ShaderHasError(penumbraShader),
                    "MovementLab palette penumbra shader is missing or has compile errors.");
                Require(presentationShader != null && !ShaderUtil.ShaderHasError(presentationShader),
                    "MovementLab native pixel presentation shader is missing or has compile errors.");
                Require(NativePixelPresentation.PixelsPerUnit == 16 &&
                    NativePixelViewportMath.MaximumLogicalDimension == 1072 &&
                    NativePixelPresentation.FullyVisibleRadiusPixels == 456 &&
                    NativePixelPresentation.PenumbraThicknessPixels == 64 &&
                    NativePixelPresentation.FullDarknessRadiusPixels == 520,
                    "MovementLab native pixel presentation constants changed.");
                Require(GetComponentsInScene<PixelPerfectCamera>(scene).Count == 0,
                    "MovementLab must not retain the old 480x270 Pixel Perfect Camera path.");
                Require(FindInScene<MovementLabRespawn>(scene) != null,
                    "MovementLab failsafe respawn is missing.");

                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
                string rendererYaml = File.ReadAllText(Path.Combine(projectRoot, Renderer2DPath));
                Require(rendererYaml.Contains("m_UseDepthStencilBuffer: 0"),
                    "Renderer2D depth/stencil buffer must remain disabled.");
            }
            finally
            {
                if (openedForValidation)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
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

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in transforms)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
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

        private static Camera FindCamera(Scene scene, string name)
        {
            return GetComponentsInScene<Camera>(scene).FirstOrDefault(camera => camera.name == name);
        }

        private static List<T> GetComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static int CountOccupiedCells(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            return tilemap.GetTilesBlock(bounds).Count(tile => tile != null);
        }

        private static IEnumerable<Vector3Int> GetCourseCells()
        {
            foreach (CourseBlock block in Course)
            {
                for (int x = block.Left; x < block.Left + block.Width; x++)
                {
                    for (int y = block.Top - block.Depth; y < block.Top; y++)
                    {
                        yield return new Vector3Int(x, y, 0);
                    }
                }
            }
        }

        private static int EnsureGroundLayer()
        {
            return EnsureLayer("Ground", 6);
        }

        private static int EnsureLayer(string layerName, int preferredLayer)
        {
            UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject serialized = new SerializedObject(tagManager);
            SerializedProperty layers = serialized.FindProperty("layers");
            for (int index = 0; index < layers.arraySize; index++)
            {
                if (layers.GetArrayElementAtIndex(index).stringValue == layerName)
                {
                    return index;
                }
            }

            SerializedProperty slot = layers.GetArrayElementAtIndex(preferredLayer);
            Require(
                string.IsNullOrEmpty(slot.stringValue),
                $"Unity layer {preferredLayer} is occupied; assign an empty layer to {layerName}.");
            slot.stringValue = layerName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return preferredLayer;
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null, $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null, $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null, $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(UnityEngine.Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null, $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
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
