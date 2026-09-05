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
        private const string JumpDustSpritePath = "Assets/Art/Effects/Movement/player_jump_dust.png";
        private const string JumpDustPrefabPath = "Assets/Prefabs/Effects/Movement/PlayerJumpDust.prefab";
        private const string ControllerPath = "Assets/Art/Characters/Player/Animations/PlayerGameplay.controller";
        private const string BodyAnimationRoot = "Assets/Art/Characters/Player/Animations/Body";
        private const string BodySpriteRoot = "Assets/Art/Characters/Player/Sprites/Body";
        private const string UnarmedArmsSpriteRoot = "Assets/Art/Characters/Player/Sprites/Arms/Unarmed";
        private const string LongwatchIdleAimRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim/Idle";
        private const string LongwatchRunAimRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim/Run";
        private const string LongwatchBackpedalAimRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim/Backpedal";
        private const string CollisionTilePath = "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";
        private const string RuleTilePath = "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private const string PenumbraShaderPath = "Assets/Shaders/RustlinePalettePenumbra.shader";
        private const string PresentationShaderPath = "Assets/Shaders/RustlineNativePixelPresent.shader";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string UniversalRpPath = "Assets/Settings/UniversalRP.asset";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

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

        private static readonly string[] LongwatchDirectionSuffixes =
        {
            "p90", "p80", "p70", "p60", "p50", "p40", "p30", "p20", "p10", "0",
            "m10", "m20", "m30", "m40", "m50", "m60", "m70", "m80", "m90",
        };

        private static readonly int[] LongwatchDirectionAngles =
        {
            90, 80, 70, 60, 50, 40, 30, 20, 10, 0,
            -10, -20, -30, -40, -50, -60, -70, -80, -90,
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
            EnsureFolder("Assets/Prefabs/Effects/Movement");
            int groundLayer = EnsureGroundLayer();
            ConfigureRenderer2DDefaultMaterial();

            PlayerMovementConfig config = CreateConfig();
            PhysicsMaterial2D physicsMaterial = CreatePhysicsMaterial();
            AnimatorController controller = CreateGameplayController();
            Tile collisionTile = CreateCollisionTile();
            PlayerJumpDustFx2D jumpDustPrefab = CreateJumpDustPrefab();
            GameObject prefab = CreatePlayerPrefab(config, physicsMaterial, controller, jumpDustPrefab, groundLayer);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateMovementLab(config, prefab, collisionTile, groundLayer);
            }
            WireMovementLabLongwatchPresentation();
            ConfigureMovementLabIdentityUnlitRendering();
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
            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(child.stateMachine);
            }

            string[] stateNames = { "Idle", "Run", "Backpedal", "Jump", "Fall", "Land" };
            HashSet<string> requiredStates = new HashSet<string>(stateNames);
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                if (!requiredStates.Contains(child.state.name))
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (string stateName in stateNames)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    BodyAnimationRoot + "/Player_Body_" + stateName + ".anim");
                Require(clip != null, "Missing accepted layered body animation clip: Player_Body_" + stateName);
                AnimatorState state = stateMachine.states
                    .Select(child => child.state)
                    .FirstOrDefault(candidate => candidate.name == stateName);
                if (state == null)
                {
                    state = stateMachine.AddState(stateName);
                }
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
            PlayerJumpDustFx2D jumpDustPrefab,
            int groundLayer)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Require(inputActions != null, "Input action asset is missing: " + InputPath);
            List<Sprite> bodyFrames = LoadLayeredPlayerFrames(BodySpriteRoot, "body");
            List<Sprite> armsFrames = LoadLayeredPlayerFrames(UnarmedArmsSpriteRoot, "arms");
            List<Sprite> bodyIdleFrames = LoadSpritesByFrameIndex(
                BodySpriteRoot + "/player_salvager_body_idle.png");
            List<Sprite> bodyRunFrames = LoadSpritesByFrameIndex(
                BodySpriteRoot + "/player_salvager_body_run.png");
            List<Sprite> bodyBackpedalFrames = LoadSpritesByFrameIndex(
                BodySpriteRoot + "/player_salvager_body_backpedal.png");
            List<Sprite> longwatchIdleFrames = LoadLongwatchAimFrames(
                LongwatchIdleAimRoot, "idle", 2);
            List<Sprite> longwatchRunFrames = LoadLongwatchAimFrames(
                LongwatchRunAimRoot, "run", 6);
            List<Sprite> longwatchBackpedalFrames = LoadLongwatchAimFrames(
                LongwatchBackpedalAimRoot, "backpedal", 4);
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "URP Sprite-Unlit-Default material is missing.");
            Require(bodyFrames.Count == 18 && armsFrames.Count == 18,
                "The player prefab requires all 18 Body and Unarmed Arms frames.");
            Require(bodyIdleFrames.Count == 2 && bodyRunFrames.Count == 6 &&
                bodyBackpedalFrames.Count == 4 && longwatchIdleFrames.Count == 38 &&
                longwatchRunFrames.Count == 114 && longwatchBackpedalFrames.Count == 76,
                "The Longwatch presenter requires complete Idle, Run, and Backpedal Body/armed frames.");

            bool editingExistingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null;
            GameObject root = editingExistingPrefab
                ? PrefabUtility.LoadPrefabContents(PlayerPrefabPath)
                : new GameObject("Player");
            try
            {
                if (!editingExistingPrefab)
                {
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
                }

                PlayerInputReader input = GetOrAddComponent<PlayerInputReader>(root);
                SetObjectReference(input, "inputActions", inputActions);
                PlayerGroundProbe2D probe = GetOrAddComponent<PlayerGroundProbe2D>(root);
                SetObjectReference(probe, "config", config);
                SetInteger(probe, "groundLayers", 1 << groundLayer);
                PlayerMotor2D motor = GetOrAddComponent<PlayerMotor2D>(root);
                SetObjectReference(motor, "config", config);

                Transform visualTransform = root.transform.Find("Visual - 48x64 Full Cell");
                GameObject visual = visualTransform != null
                    ? visualTransform.gameObject
                    : new GameObject("Visual - 48x64 Full Cell");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);

                GameObject aimOriginObject = GetOrCreateChild(visual.transform, "AimOrigin");
                aimOriginObject.transform.localPosition = new Vector3(0f, PlayerAim2D.AimOriginOffsetWorldUnits, 0f);
                aimOriginObject.transform.SetSiblingIndex(0);

                SpriteRenderer legacyRenderer = visual.GetComponent<SpriteRenderer>();
                Animator legacyAnimator = visual.GetComponent<Animator>();
                if (legacyRenderer != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacyRenderer);
                }
                if (legacyAnimator != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacyAnimator);
                }

                GameObject bodyVisual = GetOrCreateChild(visual.transform, "BodySpriteRenderer");
                SpriteRenderer bodyRenderer = GetOrAddComponent<SpriteRenderer>(bodyVisual);
                bodyRenderer.sprite = bodyFrames[0];
                bodyRenderer.sharedMaterial = unlitMaterial;
                bodyRenderer.sortingOrder = 10;
                Animator animator = GetOrAddComponent<Animator>(bodyVisual);
                animator.runtimeAnimatorController = controller;

                GameObject armsVisual = GetOrCreateChild(visual.transform, "ArmsWeaponSpriteRenderer");
                SpriteRenderer armsRenderer = GetOrAddComponent<SpriteRenderer>(armsVisual);
                armsRenderer.sprite = armsFrames[0];
                armsRenderer.sharedMaterial = unlitMaterial;
                armsRenderer.sortingOrder = 11;

                PlayerUnarmedArmsPresenter2D armsPresenter = GetOrAddComponent<PlayerUnarmedArmsPresenter2D>(root);
                RustlineM0ArtSetup.ConfigureLayeredPresenter(
                    armsPresenter, bodyRenderer, armsRenderer, bodyFrames, armsFrames);

                PlayerAim2D playerAim = GetOrAddComponent<PlayerAim2D>(root);
                SetObjectReference(playerAim, "input", input);
                SetObjectReference(playerAim, "aimOrigin", aimOriginObject.transform);
                SetObjectReference(playerAim, "nativePixelPresentation", null);

                PlayerAnimator2D presentation = GetOrAddComponent<PlayerAnimator2D>(root);
                SetObjectReference(presentation, "config", config);
                SetObjectReference(presentation, "animator", animator);
                SetObjectReference(presentation, "bodySpriteRenderer", bodyRenderer);
                SetObjectReference(presentation, "armsWeaponSpriteRenderer", armsRenderer);
                SetObjectReference(presentation, "playerAim", playerAim);

                PlayerJumpPresentation2D jumpPresentation = GetOrAddComponent<PlayerJumpPresentation2D>(root);
                SetObjectReference(jumpPresentation, "visual", visual.transform);
                SetObjectReference(jumpPresentation, "bodySpriteRenderer", bodyRenderer);
                SetObjectReference(jumpPresentation, "jumpDustPrefab", jumpDustPrefab);

                PlayerLongwatchAimPresenter2D longwatchPresenter =
                    GetOrAddComponent<PlayerLongwatchAimPresenter2D>(root);
                ConfigureLongwatchPresenter(
                    longwatchPresenter,
                    playerAim,
                    presentation,
                    armsPresenter,
                    bodyRenderer,
                    armsRenderer,
                    bodyIdleFrames,
                    longwatchIdleFrames,
                    bodyRunFrames,
                    longwatchRunFrames,
                    bodyBackpedalFrames,
                    longwatchBackpedalFrames);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Require(prefab != null, "Failed to create the player prefab.");
                return prefab;
            }
            finally
            {
                if (editingExistingPrefab)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static PlayerJumpDustFx2D CreateJumpDustPrefab()
        {
            List<Sprite> frames = AssetDatabase.LoadAllAssetsAtPath(JumpDustSpritePath)
                .OfType<Sprite>()
                .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                .ToList();
            Require(frames.Count == 3, "Jump dust prefab requires exactly three imported sprites.");
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "URP Sprite-Unlit-Default material is missing.");

            bool editingExistingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JumpDustPrefabPath) != null;
            GameObject root = editingExistingPrefab
                ? PrefabUtility.LoadPrefabContents(JumpDustPrefabPath)
                : new GameObject("PlayerJumpDust");
            try
            {
                root.name = "PlayerJumpDust";
                SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(root);
                renderer.sprite = frames[0];
                renderer.sharedMaterial = unlitMaterial;
                renderer.sortingOrder = 9;

                PlayerJumpDustFx2D effect = GetOrAddComponent<PlayerJumpDustFx2D>(root);
                SetObjectReference(effect, "spriteRenderer", renderer);
                SetObjectReferenceArray(effect, "frames", frames);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, JumpDustPrefabPath);
                Require(prefab != null, "Failed to create the jump dust prefab.");
                return prefab.GetComponent<PlayerJumpDustFx2D>();
            }
            finally
            {
                if (editingExistingPrefab)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.localPosition = Vector3.zero;
                existing.localRotation = Quaternion.identity;
                existing.localScale = Vector3.one;
                return existing.gameObject;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static List<Sprite> LoadLayeredPlayerFrames(string spriteRoot, string layerId)
        {
            string[] states = { "idle", "run", "backpedal", "jump", "fall", "land" };
            List<Sprite> frames = new List<Sprite>(18);
            foreach (string state in states)
            {
                string path = spriteRoot + "/player_salvager_" + layerId + "_" + state + ".png";
                List<Sprite> stateFrames = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                    .ToList();
                Require(stateFrames.Count > 0, "Layered player sprites are missing: " + path);
                frames.AddRange(stateFrames);
            }

            return frames;
        }

        private static List<Sprite> LoadLongwatchAimFrames(
            string root,
            string stateId,
            int frameCount)
        {
            List<Sprite> frames = new List<Sprite>(LongwatchDirectionSuffixes.Length * frameCount);
            foreach (string suffix in LongwatchDirectionSuffixes)
            {
                string path = root + "/player_salvager_longwatch_dmr_" + stateId + "_aim_" + suffix + ".png";
                List<Sprite> directionFrames = LoadSpritesByFrameIndex(path);
                Require(directionFrames.Count == frameCount,
                    $"Longwatch {stateId} direction must contain exactly {frameCount} frames: {path}");
                frames.AddRange(directionFrames);
            }

            return frames;
        }

        private static List<Sprite> LoadSpritesByFrameIndex(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                .ToList();
        }

        private static void ConfigureLongwatchPresenter(
            PlayerLongwatchAimPresenter2D presenter,
            PlayerAim2D playerAim,
            PlayerAnimator2D playerAnimator,
            PlayerUnarmedArmsPresenter2D unarmedPresenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            IReadOnlyList<Sprite> bodyIdleFrames,
            IReadOnlyList<Sprite> longwatchIdleFrames,
            IReadOnlyList<Sprite> bodyRunFrames,
            IReadOnlyList<Sprite> longwatchRunFrames,
            IReadOnlyList<Sprite> bodyBackpedalFrames,
            IReadOnlyList<Sprite> longwatchBackpedalFrames)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("playerAim").objectReferenceValue = playerAim;
            serialized.FindProperty("playerAnimator").objectReferenceValue = playerAnimator;
            serialized.FindProperty("unarmedPresenter").objectReferenceValue = unarmedPresenter;
            serialized.FindProperty("bodySpriteRenderer").objectReferenceValue = bodyRenderer;
            serialized.FindProperty("armsWeaponSpriteRenderer").objectReferenceValue = armsRenderer;

            SerializedProperty bodyFrames = serialized.FindProperty("bodyIdleFrames");
            bodyFrames.arraySize = bodyIdleFrames.Count;
            for (int index = 0; index < bodyIdleFrames.Count; index++)
            {
                bodyFrames.GetArrayElementAtIndex(index).objectReferenceValue = bodyIdleFrames[index];
            }

            SerializedProperty idlePoses = serialized.FindProperty("idleAimPoses");
            idlePoses.arraySize = LongwatchDirectionAngles.Length;
            for (int index = 0; index < LongwatchDirectionAngles.Length; index++)
            {
                SerializedProperty pose = idlePoses.GetArrayElementAtIndex(index);
                pose.FindPropertyRelative("angleDegrees").intValue = LongwatchDirectionAngles[index];
                pose.FindPropertyRelative("frame0").objectReferenceValue = longwatchIdleFrames[index * 2];
                pose.FindPropertyRelative("frame1").objectReferenceValue = longwatchIdleFrames[index * 2 + 1];
            }

            SerializedProperty runBodyFrames = serialized.FindProperty("bodyRunFrames");
            runBodyFrames.arraySize = bodyRunFrames.Count;
            for (int index = 0; index < bodyRunFrames.Count; index++)
            {
                runBodyFrames.GetArrayElementAtIndex(index).objectReferenceValue = bodyRunFrames[index];
            }

            SerializedProperty runPoses = serialized.FindProperty("runAimPoses");
            runPoses.arraySize = LongwatchDirectionAngles.Length;
            for (int directionIndex = 0; directionIndex < LongwatchDirectionAngles.Length; directionIndex++)
            {
                SerializedProperty pose = runPoses.GetArrayElementAtIndex(directionIndex);
                pose.FindPropertyRelative("angleDegrees").intValue = LongwatchDirectionAngles[directionIndex];
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    pose.FindPropertyRelative("frame" + frameIndex).objectReferenceValue =
                        longwatchRunFrames[directionIndex * 6 + frameIndex];
                }
            }

            SerializedProperty backpedalBodyFrames = serialized.FindProperty("bodyBackpedalFrames");
            backpedalBodyFrames.arraySize = bodyBackpedalFrames.Count;
            for (int index = 0; index < bodyBackpedalFrames.Count; index++)
            {
                backpedalBodyFrames.GetArrayElementAtIndex(index).objectReferenceValue = bodyBackpedalFrames[index];
            }

            SerializedProperty backpedalPoses = serialized.FindProperty("backpedalAimPoses");
            backpedalPoses.arraySize = LongwatchDirectionAngles.Length;
            for (int directionIndex = 0; directionIndex < LongwatchDirectionAngles.Length; directionIndex++)
            {
                SerializedProperty pose = backpedalPoses.GetArrayElementAtIndex(directionIndex);
                pose.FindPropertyRelative("angleDegrees").intValue = LongwatchDirectionAngles[directionIndex];
                for (int frameIndex = 0; frameIndex < 4; frameIndex++)
                {
                    pose.FindPropertyRelative("frame" + frameIndex).objectReferenceValue =
                        longwatchBackpedalFrames[directionIndex * 4 + frameIndex];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireMovementLabLongwatchPresentation()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            PlayerAim2D playerAim = FindInScene<PlayerAim2D>(scene);
            NativePixelPresentation nativePresentation = FindInScene<NativePixelPresentation>(scene);
            Require(playerAim != null,
                "MovementLab player is missing the generic aim component.");
            Require(nativePresentation != null,
                "MovementLab is missing its native-pixel presentation.");
            SetObjectReference(playerAim, "nativePixelPresentation", nativePresentation);
            PrefabUtility.RemoveUnusedOverrides(
                new[] { playerAim.transform.root.gameObject },
                InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void ConfigureRenderer2DDefaultMaterial()
        {
            UnityEngine.Object rendererData = AssetDatabase.LoadMainAssetAtPath(Renderer2DPath);
            Require(rendererData != null, "Renderer2D data asset is missing.");
            SerializedObject serialized = new SerializedObject(rendererData);
            SerializedProperty defaultMaterialType = serialized.FindProperty("m_DefaultMaterialType");
            Require(defaultMaterialType != null,
                "Renderer2D default material selection is unavailable in this URP version.");
            if (defaultMaterialType.intValue != 1)
            {
                defaultMaterialType.intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rendererData);
            }
        }

        private static void ConfigureMovementLabIdentityUnlitRendering()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "URP Sprite-Unlit-Default material is missing.");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TilemapRenderer renderer in root.GetComponentsInChildren<TilemapRenderer>(true))
                {
                    renderer.sharedMaterial = unlitMaterial;
                    EditorUtility.SetDirty(renderer);
                }
            }

            GameObject identityLight = FindGameObject(scene, "Global Light 2D");
            if (identityLight != null)
            {
                UnityEngine.Object.DestroyImmediate(identityLight);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static int ParseTrailingIndex(string name)
        {
            int separator = name.LastIndexOf('_');
            int index = -1;
            Require(separator >= 0 && int.TryParse(name.Substring(separator + 1), out index),
                "Layered player sprite name must end in a numeric frame index: " + name);
            return index;
        }

        private static void CreateMovementLab(
            PlayerMovementConfig config,
            GameObject playerPrefab,
            TileBase collisionTile,
            int groundLayer)
        {
            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Require(ruleTile != null, "Accepted M0 IndustrialSurface Rule Tile is missing.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MovementLab";
            GameObject root = new GameObject("RUSTLINE M1A - MOVEMENT LAB");

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

            CreateCamera(root.transform, player.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void CreateCamera(Transform parent, Transform target)
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

            NativePixelPresentation presentation = driverCameraObject.AddComponent<NativePixelPresentation>();
            SetObjectReference(presentation, "worldCamera", worldCamera);
            SetObjectReference(presentation, "processingCamera", driverCamera);
            SetObjectReference(presentation, "playerTarget", target);
            SetObjectReference(presentation, "penumbraShader", penumbraShader);
            SetObjectReference(presentation, "presentationShader", presentationShader);
            SetBoolean(presentation, "penumbraEnabled", true);
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
            Require(Mathf.Approximately(config.MaxGroundSpeed, 7f) &&
                Mathf.Approximately(config.MaxBackpedalGroundSpeed, 4f) &&
                Mathf.Approximately(config.MaxAirSpeed, 7f),
                "Player forward/backpedal/air speed defaults changed from 7/4/7.");
            Require(Mathf.Approximately(config.LandPresentationDuration, 0.22f),
                "Player Land presentation duration must remain 0.22 seconds.");

            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputActionMap playerMap = input?.FindActionMap("Player", false);
            Require(input != null && input.actionMaps.Count == 1 && playerMap != null,
                "Input asset must contain only the focused Player action map for M1A.");
            Require(playerMap.actions.Count == 3 && playerMap.FindAction("Move", false) != null &&
                playerMap.FindAction("Jump", false) != null &&
                playerMap.FindAction("PointerPosition", false) != null,
                "Player input must contain Move, Jump, and PointerPosition.");
            Require(playerMap.FindAction("Move").bindings.Any(binding => binding.path == "<Gamepad>/dpad"),
                "Move must support the gamepad D-pad.");
            Require(playerMap.FindAction("Jump").bindings.Any(binding => binding.path == "<Keyboard>/space") &&
                playerMap.FindAction("Jump").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"),
                "Jump bindings are incomplete.");
            InputAction pointerPosition = playerMap.FindAction("PointerPosition");
            Require(pointerPosition.type == InputActionType.PassThrough &&
                pointerPosition.expectedControlType == "Vector2" &&
                pointerPosition.bindings.Count == 1 &&
                pointerPosition.bindings[0].path == "<Pointer>/position",
                "PointerPosition must be a Vector2 PassThrough action bound only to <Pointer>/position.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Require(prefab != null, "Player prefab is missing.");
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = prefab.GetComponent<CapsuleCollider2D>();
            Require(body != null && body.bodyType == RigidbodyType2D.Dynamic && Mathf.Approximately(body.gravityScale, 0f),
                "Player must use a dynamic Rigidbody2D with motor-controlled gravity.");
            Require(collider != null && collider.direction == CapsuleDirection2D.Vertical &&
                collider.size == new Vector2(1.05f, 2.75f) && collider.offset == new Vector2(0f, 1.375f),
                "Player collision shape changed from the stable full-cell-relative contract.");
            PlayerAim2D playerAim = prefab.GetComponent<PlayerAim2D>();
            PlayerAnimator2D playerAnimator = prefab.GetComponent<PlayerAnimator2D>();
            Require(prefab.GetComponent<PlayerInputReader>() != null && prefab.GetComponent<PlayerGroundProbe2D>() != null &&
                prefab.GetComponent<PlayerMotor2D>() != null && prefab.GetComponent<PlayerAnimator2D>() != null &&
                playerAim != null,
                "Player prefab movement components are incomplete.");
            Require(playerAnimator.PlayerAim == playerAim,
                "Player animator must consume the generic aim-facing source.");
            PlayerUnarmedArmsPresenter2D armsPresenter = prefab.GetComponent<PlayerUnarmedArmsPresenter2D>();
            Require(armsPresenter != null && armsPresenter.MappingCount == 18 && armsPresenter.OwnsRenderer,
                "Player prefab must contain the complete active unarmed arms presenter.");
            PlayerLongwatchAimPresenter2D longwatchPresenter =
                prefab.GetComponent<PlayerLongwatchAimPresenter2D>();
            PlayerJumpPresentation2D jumpPresentation = prefab.GetComponent<PlayerJumpPresentation2D>();
            Transform visual = prefab.transform.Find("Visual - 48x64 Full Cell");
            Require(visual != null && Vector3.Distance(visual.localPosition, new Vector3(0f, -0.25f, 0f)) < 0.0001f,
                "Player visual child must be lowered exactly 4 source pixels (-0.25 Unity units).");
            Transform bodyVisual = visual.Find("BodySpriteRenderer");
            Transform armsVisual = visual.Find("ArmsWeaponSpriteRenderer");
            Transform aimOrigin = visual.Find("AimOrigin");
            SpriteRenderer bodyRenderer = bodyVisual?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = armsVisual?.GetComponent<SpriteRenderer>();
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(bodyVisual != null && armsVisual != null && bodyVisual.localPosition == Vector3.zero &&
                armsVisual.localPosition == Vector3.zero && bodyVisual.localScale == Vector3.one &&
                armsVisual.localScale == Vector3.one,
                "Player Body and ArmsWeapon layers must share the same zero-offset, integer-scale visual space.");
            Require(aimOrigin != null &&
                Vector3.Distance(aimOrigin.localPosition, new Vector3(0f, 2.375f, 0f)) < 0.0001f &&
                playerAim.Input == prefab.GetComponent<PlayerInputReader>() && playerAim.AimOrigin == aimOrigin &&
                playerAim.NativePixelPresentation == null &&
                Mathf.Approximately(PlayerAim2D.AimOriginOffsetWorldUnits, 2.375f),
                "Player prefab generic aim origin or scene-only dependency contract is invalid.");
            Require(bodyRenderer != null && armsRenderer != null && bodyRenderer.sortingOrder == 10 &&
                armsRenderer.sortingOrder == 11 && armsPresenter.BodySpriteRenderer == bodyRenderer &&
                armsPresenter.ArmsWeaponSpriteRenderer == armsRenderer,
                "Player layered renderer references or sorting contract are invalid.");
            Require(unlitMaterial != null && bodyRenderer.sharedMaterial == unlitMaterial &&
                armsRenderer.sharedMaterial == unlitMaterial,
                "Player Body and ArmsWeapon renderers must use URP Sprite-Unlit-Default.");
            Require(longwatchPresenter != null && longwatchPresenter.PlayerAim == playerAim &&
                longwatchPresenter.PlayerAnimator == prefab.GetComponent<PlayerAnimator2D>() &&
                longwatchPresenter.UnarmedPresenter == armsPresenter &&
                longwatchPresenter.BodySpriteRenderer == bodyRenderer &&
                longwatchPresenter.ArmsWeaponSpriteRenderer == armsRenderer &&
                longwatchPresenter.BodyIdleFrameCount == 2 && longwatchPresenter.IdleAimPoseCount == 19 &&
                longwatchPresenter.BodyRunFrameCount == 6 && longwatchPresenter.RunAimPoseCount == 19 &&
                longwatchPresenter.BodyBackpedalFrameCount == 4 &&
                longwatchPresenter.BackpedalAimPoseCount == 19,
                "Player prefab Longwatch Idle/Run/Backpedal presenter wiring is incomplete.");
            for (int index = 0; index < LongwatchDirectionAngles.Length; index++)
            {
                LongwatchIdleAimPose idlePose = longwatchPresenter.GetIdleAimPose(index);
                Require(idlePose.AngleDegrees == LongwatchDirectionAngles[index] &&
                    idlePose.Frame0 != null && idlePose.Frame1 != null &&
                    idlePose.Frame0.name.EndsWith("_" + LongwatchDirectionSuffixes[index] + "_0") &&
                    idlePose.Frame1.name.EndsWith("_" + LongwatchDirectionSuffixes[index] + "_1"),
                    "Longwatch Idle presenter pose mapping mismatch at direction " + index + ".");

                LongwatchRunAimPose runPose = longwatchPresenter.GetRunAimPose(index);
                Require(runPose.AngleDegrees == LongwatchDirectionAngles[index],
                    "Longwatch Run presenter angle mismatch at direction " + index + ".");
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    Sprite frame = runPose.GetFrame(frameIndex);
                    Require(frame != null &&
                        frame.name.EndsWith("_" + LongwatchDirectionSuffixes[index] + "_" + frameIndex),
                        $"Longwatch Run presenter pose mismatch at direction {index}, frame {frameIndex}.");
                }

                LongwatchBackpedalAimPose backpedalPose = longwatchPresenter.GetBackpedalAimPose(index);
                Require(backpedalPose.AngleDegrees == LongwatchDirectionAngles[index],
                    "Longwatch Backpedal presenter angle mismatch at direction " + index + ".");
                for (int frameIndex = 0; frameIndex < 4; frameIndex++)
                {
                    Sprite frame = backpedalPose.GetFrame(frameIndex);
                    Require(frame != null &&
                        frame.name.EndsWith("_" + LongwatchDirectionSuffixes[index] + "_" + frameIndex),
                        $"Longwatch Backpedal presenter pose mismatch at direction {index}, frame {frameIndex}.");
                }
            }
            PlayerJumpDustFx2D jumpDustPrefab = AssetDatabase.LoadAssetAtPath<PlayerJumpDustFx2D>(JumpDustPrefabPath);
            Require(jumpPresentation != null && jumpPresentation.Visual == visual &&
                jumpPresentation.BodySpriteRenderer == bodyRenderer &&
                jumpPresentation.JumpDustPrefab == jumpDustPrefab,
                "Player jump presentation references are incomplete.");
            Require(jumpDustPrefab != null && jumpDustPrefab.FrameCount == 3 &&
                jumpDustPrefab.SpriteRenderer != null && jumpDustPrefab.SpriteRenderer.sortingOrder == 9,
                "Jump dust prefab must contain the three-frame one-shot renderer at sorting order 9.");
            Require(jumpDustPrefab.SpriteRenderer.sharedMaterial == unlitMaterial,
                "Jump dust renderer must use URP Sprite-Unlit-Default.");
            for (int index = 0; index < 3; index++)
            {
                Require(jumpDustPrefab.GetFrame(index) != null &&
                    jumpDustPrefab.GetFrame(index).name == "player_jump_dust_" + index,
                    "Jump dust prefab sprite order mismatch at frame " + index + ".");
            }
            Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
            Require(animators.Length == 1 && animators[0].transform == bodyVisual,
                "The body layer must contain the player's only Animator.");
            List<Sprite> expectedBodyFrames = LoadLayeredPlayerFrames(BodySpriteRoot, "body");
            List<Sprite> expectedArmsFrames = LoadLayeredPlayerFrames(UnarmedArmsSpriteRoot, "arms");
            HashSet<Sprite> mappedArms = new HashSet<Sprite>();
            for (int index = 0; index < expectedBodyFrames.Count; index++)
            {
                Require(armsPresenter.TryGetArmsSprite(expectedBodyFrames[index], out Sprite mappedArmsSprite) &&
                    mappedArmsSprite == expectedArmsFrames[index],
                    "Player body-to-arms mapping is missing or incorrect at frame " + index + ".");
                Require(mappedArms.Add(mappedArmsSprite),
                    "Player body-to-arms mapping contains a duplicate Arms sprite at frame " + index + ".");
            }

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
                Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
                Require(unlitMaterial != null &&
                    visualTilemap.GetComponent<TilemapRenderer>()?.sharedMaterial == unlitMaterial &&
                    collisionTilemap.GetComponent<TilemapRenderer>()?.sharedMaterial == unlitMaterial,
                    "MovementLab Tilemap renderers must use URP Sprite-Unlit-Default.");
                Require(FindGameObject(scene, "Global Light 2D") == null,
                    "MovementLab must not retain the obsolete identity Global Light 2D.");

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
                Camera driverCamera = FindCamera(scene, "Native Pixel Driver Camera");
                Require(GetComponentsInScene<Camera>(scene).Count == 2,
                    "MovementLab must contain only the world camera and the native-pixel RenderGraph driver camera.");
                Require(worldCamera != null && worldCamera.orthographic && !worldCamera.allowHDR &&
                    !worldCamera.allowMSAA && worldCamera.CompareTag("MainCamera"),
                    "MovementLab logical world camera configuration is invalid.");
                Require(worldCamera.GetComponent<PixelCameraFollow2D>() != null,
                    "MovementLab camera follow is missing.");
                Require(driverCamera != null && driverCamera.orthographic &&
                    driverCamera.cullingMask == 0 &&
                    !driverCamera.allowHDR && !driverCamera.allowMSAA &&
                    driverCamera.targetTexture == null && !driverCamera.enabled &&
                    driverCamera.clearFlags == CameraClearFlags.Nothing &&
                    Mathf.Approximately(driverCamera.depth, 5f),
                    "MovementLab native-pixel driver camera configuration is invalid.");

                Require(FindGameObject(scene, "Logical Penumbra Pass Quad") == null,
                    "MovementLab must not retain the legacy logical penumbra quad.");
                Require(FindGameObject(scene, "Physical Native Pixel Presentation Quad") == null,
                    "MovementLab must not retain the legacy physical presentation quad.");
                Require(FindCamera(scene, "Presentation Camera - Deep Space Surround") == null,
                    "MovementLab must not retain the legacy physical presentation camera.");

                NativePixelPresentation presentation = FindInScene<NativePixelPresentation>(scene);
                PlayerLongwatchAimPresenter2D longwatchPresenter =
                    FindInScene<PlayerLongwatchAimPresenter2D>(scene);
                PlayerAim2D playerAim = FindInScene<PlayerAim2D>(scene);
                Shader penumbraShader = AssetDatabase.LoadAssetAtPath<Shader>(PenumbraShaderPath);
                Shader presentationShader = AssetDatabase.LoadAssetAtPath<Shader>(PresentationShaderPath);
                Require(presentation != null && presentation.gameObject == driverCamera.gameObject &&
                    presentation.WorldCamera == worldCamera &&
                    presentation.ProcessingCamera == driverCamera && presentation.PlayerTarget != null &&
                    presentation.PlayerTarget.name == "Player - Movement Specimen" &&
                    presentation.PenumbraShader == penumbraShader &&
                    presentation.PresentationShader == presentationShader &&
                    presentation.PenumbraEnabled,
                    "MovementLab native pixel presentation references/defaults are invalid.");
                Require(longwatchPresenter != null && playerAim != null &&
                    longwatchPresenter.PlayerAim == playerAim &&
                    playerAim.NativePixelPresentation == presentation,
                    "MovementLab generic aim must own the serialized native-pixel presentation reference.");
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

                UniversalRenderPipelineAsset pipelineAsset =
                    AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UniversalRpPath);
                Require(pipelineAsset != null &&
                    !pipelineAsset.supportsCameraDepthTexture &&
                    !pipelineAsset.supportsCameraOpaqueTexture &&
                    !pipelineAsset.supportsHDR &&
                    !pipelineAsset.supportsTerrainHoles &&
                    !pipelineAsset.enableLODCrossFade &&
                    pipelineAsset.mainLightRenderingMode == LightRenderingMode.Disabled &&
                    pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.Disabled &&
                    !pipelineAsset.supportsMainLightShadows &&
                    !pipelineAsset.supportsAdditionalLightShadows &&
                    !pipelineAsset.supportsMixedLighting &&
                    !pipelineAsset.supportsLightCookies &&
                    !pipelineAsset.supportDataDrivenLensFlare &&
                    !pipelineAsset.supportScreenSpaceLensFlare &&
                    !pipelineAsset.useAdaptivePerformance &&
                    pipelineAsset.volumeFrameworkUpdateMode == VolumeFrameworkUpdateMode.ViaScripting,
                    "Rustline's unused URP 3D/HDR/Volume capabilities must remain pruned.");
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

        private static void SetObjectReferenceArray<T>(UnityEngine.Object target, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null && property.isArray,
                $"Serialized array property {propertyName} is missing on {target.GetType().Name}.");
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

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
