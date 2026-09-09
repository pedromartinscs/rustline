using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rustline.Diagnostics;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Physics;
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
        private const string LongwatchDefinitionPath = "Assets/Config/Weapons/LongwatchDMR.asset";
        private const string LongwatchMuzzleMetadataJsonPath =
            "ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json";
        private const string LongwatchMuzzleMetadataAssetPath =
            "Assets/Config/Weapons/Generated/LongwatchDMRMuzzleMetadata.asset";
        private const string LongwatchMuzzleFlashPath =
            "Assets/Art/Effects/Weapons/longwatch_dmr/longwatch_dmr_muzzle_flash.png";
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
        private const string LongwatchCrouchAimRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/longwatch_dmr/Aim/Crouch";
        private const string CollisionTilePath = "Assets/Art/Environment/Tiles/Generated/MovementCollisionTile.asset";
        private const string RuleTilePath = "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private const string PenumbraShaderPath = "Assets/Shaders/RustlinePalettePenumbra.shader";
        private const string PresentationShaderPath = "Assets/Shaders/RustlineNativePixelPresent.shader";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string UniversalRpPath = "Assets/Settings/UniversalRP.asset";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string PrecisionCrouchCeilingName = "Combat Crouch Ceiling - Precision";
        private const int PrecisionCrouchCeilingLeft = 69;
        private const int PrecisionCrouchCeilingWidth = 9;
        private const int PrecisionCrouchCeilingCellY = 2;
        private const float CrouchClearanceRaiseWorldUnits = 10f / 16f;

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
            // Combat-crouch tunnel floor. Its ceiling is a dedicated sub-cell precision collider
            // because the accepted clearance is 10 source pixels above the old 32 px grid opening.
            new CourseBlock(64, 0, 28, 4),
            // Wall-brace tuning shaft: deep side columns and a lower recovery floor.
            new CourseBlock(92, 0, 1, 8),
            new CourseBlock(93, -5, 4, 3),
            new CourseBlock(97, 0, 15, 8),
            // Longwatch firing range floor and a full-height Ground occluder.
            new CourseBlock(112, 0, 60, 4),
            new CourseBlock(150, 5, 1, 5),
        };

        private static readonly DiagnosticLabel[] DiagnosticLabels =
        {
            new DiagnosticLabel("M1A MOVEMENT LAB", new Vector3(-27f, 6.5f, -0.2f), 0.19f,
                new Color32(32, 237, 229, 255)),
            new DiagnosticLabel("MOVE  |  SPACE JUMP  |  S/DOWN CROUCH", new Vector3(-27f, 5.9f, -0.2f), 0.095f,
                new Color32(201, 187, 177, 255)),
            new DiagnosticLabel("ACCEL / DECEL / REVERSAL", new Vector3(-22.5f, 2f, -0.2f), 0.09f,
                new Color32(201, 187, 177, 255)),
            new DiagnosticLabel("COYOTE GAPS", new Vector3(-8f, 3.2f, -0.2f), 0.09f,
                new Color32(253, 208, 69, 255)),
            new DiagnosticLabel("JUMP UP + VARIABLE HEIGHT", new Vector3(13.5f, 4.2f, -0.2f), 0.09f,
                new Color32(201, 187, 177, 255)),
            new DiagnosticLabel("DROP + BUFFER BEFORE LANDING", new Vector3(26.5f, 0f, -0.2f), 0.09f,
                new Color32(253, 208, 69, 255)),
            new DiagnosticLabel("STEP COURSE / AIR CONTROL", new Vector3(47.5f, 2.2f, -0.2f), 0.09f,
                new Color32(201, 187, 177, 255)),
            new DiagnosticLabel("COMBAT CROUCH / AUTO-STAND", new Vector3(74f, 4.4f, -0.2f), 0.09f,
                new Color32(253, 208, 69, 255)),
            new DiagnosticLabel("WALL BRACE / KICK SHAFT", new Vector3(96.5f, 2f, -0.2f), 0.09f,
                new Color32(32, 237, 229, 255)),
            new DiagnosticLabel("LONGWATCH FIRING RANGE", new Vector3(140f, 8f, -0.2f), 0.11f,
                new Color32(254, 212, 55, 255)),
            new DiagnosticLabel("FIRST COMBAT ENTITY", new Vector3(164f, 4.25f, -0.2f), 0.1f,
                new Color32(32, 237, 229, 255)),
        };

        private static readonly Vector3 PrototypeEnemySpawnPosition = new Vector3(164f, 0.02f, -0.1f);

        private static readonly CombatTargetSpec[] CombatTargets =
        {
            new CombatTargetSpec("Target - Clear Horizontal", new Vector3(124f, 2.125f, -0.1f)),
            new CombatTargetSpec("Target - Continuous +7 Degrees", new Vector3(138f, 3.1f, -0.1f)),
            new CombatTargetSpec("Target - Occluded By Ground", new Vector3(156f, 2.125f, -0.1f)),
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

        private readonly struct DiagnosticLabel
        {
            internal DiagnosticLabel(string text, Vector3 position, float size, Color color)
            {
                Text = text;
                Position = position;
                Size = size;
                Color = color;
            }

            internal string Text { get; }
            internal Vector3 Position { get; }
            internal float Size { get; }
            internal Color Color { get; }
        }

        private readonly struct CombatTargetSpec
        {
            internal CombatTargetSpec(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            internal string Name { get; }
            internal Vector3 Position { get; }
        }

        [Serializable]
        private sealed class LongwatchMuzzleJson
        {
            public int schemaVersion;
            public int generatorVersion;
            public string weaponId;
            public int[] cellSizePixels;
            public int[] pivotPixels;
            public LongwatchMuzzleReferenceJson reference;
            public LongwatchMuzzleStateJson[] states;
        }

        [Serializable]
        private sealed class LongwatchMuzzleReferenceJson
        {
            public string state;
            public int frame;
            public string path;
        }

        [Serializable]
        private sealed class LongwatchMuzzleStateJson
        {
            public string name;
            public int frameCount;
            public LongwatchMuzzleDirectionJson[] directions;
        }

        [Serializable]
        private sealed class LongwatchMuzzleDirectionJson
        {
            public string suffix;
            public int angleDegrees;
            public bool supported;
            public LongwatchMuzzleFrameJson[] frames;
        }

        [Serializable]
        private sealed class LongwatchMuzzleFrameJson
        {
            public int frame;
            public float[] muzzleOffsetPixels;
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
            EnsureFolder("Assets/Config/Weapons");
            EnsureFolder("Assets/Config/Weapons/Generated");
            EnsureFolder("Assets/Prefabs/Player");
            EnsureFolder("Assets/Prefabs/Effects/Movement");
            int groundLayer = EnsureGroundLayer();
            int combatTargetLayer = EnsureLayer("CombatTarget", 7);
            ConfigureRenderer2DDefaultMaterial();
            RustlineM0ArtSetup.ConfigureLongwatchAimSheets();
            RustlineM0ArtSetup.ConfigureLongwatchMuzzleFlash();

            PlayerMovementConfig config = CreateConfig();
            WeaponDefinition2D longwatchDefinition = CreateLongwatchDefinition();
            LongwatchMuzzleMetadata2D longwatchMuzzleMetadata = CreateLongwatchMuzzleMetadata();
            PhysicsMaterial2D physicsMaterial = CreatePhysicsMaterial();
            AnimatorController controller = CreateGameplayController();
            Tile collisionTile = CreateCollisionTile();
            PlayerJumpDustFx2D jumpDustPrefab = CreateJumpDustPrefab();
            GameObject prefab = CreatePlayerPrefab(
                config,
                longwatchDefinition,
                longwatchMuzzleMetadata,
                physicsMaterial,
                controller,
                jumpDustPrefab,
                groundLayer,
                combatTargetLayer);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateMovementLab(config, prefab, collisionTile, groundLayer, combatTargetLayer);
            }
            else
            {
                SynchronizeMovementLabCourse(collisionTile, groundLayer, combatTargetLayer);
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

            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("crouchColliderSize").vector2Value = new Vector2(1.05f, 2.375f);
            serialized.FindProperty("crouchColliderOffset").vector2Value = new Vector2(0f, 1.1875f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(config);
            return config;
        }

        private static WeaponDefinition2D CreateLongwatchDefinition()
        {
            WeaponDefinition2D definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LongwatchDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition2D>();
                definition.name = "Longwatch DMR";
                AssetDatabase.CreateAsset(definition, LongwatchDefinitionPath);
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("weaponId").stringValue = "longwatch_dmr";
            serialized.FindProperty("displayName").stringValue = "Longwatch DMR";
            serialized.FindProperty("fireMode").enumValueIndex = (int)WeaponFireMode2D.SemiAutomatic;
            serialized.FindProperty("shotInterval").floatValue = 0.25f;
            serialized.FindProperty("range").floatValue = 80f;
            serialized.FindProperty("damage").intValue = 40;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        private static LongwatchMuzzleMetadata2D CreateLongwatchMuzzleMetadata()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string absoluteJsonPath = Path.Combine(projectRoot, LongwatchMuzzleMetadataJsonPath);
            Require(File.Exists(absoluteJsonPath),
                "Generated Longwatch muzzle metadata JSON is missing: " + LongwatchMuzzleMetadataJsonPath);

            LongwatchMuzzleJson source = JsonUtility.FromJson<LongwatchMuzzleJson>(
                File.ReadAllText(absoluteJsonPath));
            ValidateLongwatchMuzzleJson(source);

            LongwatchMuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<LongwatchMuzzleMetadata2D>(LongwatchMuzzleMetadataAssetPath);
            if (metadata == null)
            {
                metadata = ScriptableObject.CreateInstance<LongwatchMuzzleMetadata2D>();
                metadata.name = "Longwatch DMR Muzzle Metadata";
                AssetDatabase.CreateAsset(metadata, LongwatchMuzzleMetadataAssetPath);
            }

            SerializedObject serialized = new SerializedObject(metadata);
            serialized.FindProperty("schemaVersion").intValue = source.schemaVersion;
            serialized.FindProperty("generatorVersion").intValue = source.generatorVersion;
            serialized.FindProperty("weaponId").stringValue = source.weaponId;
            serialized.FindProperty("cellSizePixels").vector2IntValue =
                new Vector2Int(source.cellSizePixels[0], source.cellSizePixels[1]);
            serialized.FindProperty("pivotPixels").vector2IntValue =
                new Vector2Int(source.pivotPixels[0], source.pivotPixels[1]);
            ConfigureMuzzleDirectionArray(serialized.FindProperty("idleDirections"), source.states[0]);
            ConfigureMuzzleDirectionArray(serialized.FindProperty("runDirections"), source.states[1]);
            ConfigureMuzzleDirectionArray(serialized.FindProperty("backpedalDirections"), source.states[2]);
            ConfigureMuzzleDirectionArray(serialized.FindProperty("crouchDirections"), source.states[3]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(metadata);
            return metadata;
        }

        private static void ValidateLongwatchMuzzleJson(LongwatchMuzzleJson source)
        {
            Require(source != null, "Generated Longwatch muzzle metadata JSON could not be parsed.");
            Require(source.schemaVersion == 1 && source.generatorVersion == 1 &&
                source.weaponId == "longwatch_dmr",
                "Generated Longwatch muzzle metadata schema/generator/weapon identity mismatch.");
            Require(source.cellSizePixels != null && source.cellSizePixels.Length == 2 &&
                source.cellSizePixels[0] == 80 && source.cellSizePixels[1] == 96 &&
                source.pivotPixels != null && source.pivotPixels.Length == 2 &&
                source.pivotPixels[0] == 24 && source.pivotPixels[1] == 8,
                "Generated Longwatch muzzle metadata geometry mismatch.");
            Require(source.reference != null && source.reference.state == "Idle" &&
                source.reference.frame == 0 &&
                source.reference.path ==
                    "ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png",
                "Generated Longwatch muzzle metadata reference contract mismatch.");

            string[] expectedStateNames = { "Idle", "Run", "Backpedal", "Crouch" };
            int[] expectedFrameCounts = { 2, 6, 4, 6 };
            Require(source.states != null && source.states.Length == expectedStateNames.Length,
                "Generated Longwatch muzzle metadata must contain exactly four states.");
            int supportedPointCount = 0;
            for (int stateIndex = 0; stateIndex < expectedStateNames.Length; stateIndex++)
            {
                LongwatchMuzzleStateJson state = source.states[stateIndex];
                Require(state != null && state.name == expectedStateNames[stateIndex] &&
                    state.frameCount == expectedFrameCounts[stateIndex] &&
                    state.directions != null &&
                    state.directions.Length == LongwatchDirectionSuffixes.Length,
                    "Generated Longwatch muzzle state mismatch at index " + stateIndex + ".");

                for (int directionIndex = 0;
                     directionIndex < LongwatchDirectionSuffixes.Length;
                     directionIndex++)
                {
                    LongwatchMuzzleDirectionJson direction = state.directions[directionIndex];
                    bool expectedSupported = state.name != "Crouch" || directionIndex < 16;
                    Require(direction != null &&
                        direction.suffix == LongwatchDirectionSuffixes[directionIndex] &&
                        direction.angleDegrees == LongwatchDirectionAngles[directionIndex] &&
                        direction.supported == expectedSupported && direction.frames != null,
                        $"Generated Longwatch muzzle direction mismatch at {state.name}/{directionIndex}.");

                    if (!expectedSupported)
                    {
                        Require(direction.frames.Length == 0,
                            $"Unsupported Longwatch muzzle direction {state.name}/{direction.suffix} " +
                            "must not serialize fake frame coordinates.");
                        continue;
                    }

                    Require(direction.frames.Length == state.frameCount,
                        $"Generated Longwatch muzzle frame count mismatch for {state.name}/{direction.suffix}.");
                    for (int frameIndex = 0; frameIndex < state.frameCount; frameIndex++)
                    {
                        LongwatchMuzzleFrameJson frame = direction.frames[frameIndex];
                        Require(frame != null && frame.frame == frameIndex &&
                            frame.muzzleOffsetPixels != null && frame.muzzleOffsetPixels.Length == 2 &&
                            float.IsFinite(frame.muzzleOffsetPixels[0]) &&
                            float.IsFinite(frame.muzzleOffsetPixels[1]) &&
                            IsHalfInteger(frame.muzzleOffsetPixels[0]) &&
                            IsHalfInteger(frame.muzzleOffsetPixels[1]),
                            $"Generated Longwatch muzzle coordinate mismatch at " +
                            $"{state.name}/{direction.suffix}/frame {frameIndex}.");
                        supportedPointCount++;
                    }
                }
            }

            Require(supportedPointCount == 324,
                "Generated Longwatch muzzle metadata must contain exactly 324 supported frame points.");
        }

        private static bool IsHalfInteger(float value)
        {
            return Mathf.Approximately(value * 2f, Mathf.Round(value * 2f)) &&
                   !Mathf.Approximately(value, Mathf.Round(value));
        }

        private static void ConfigureMuzzleDirectionArray(
            SerializedProperty target,
            LongwatchMuzzleStateJson source)
        {
            Require(target != null && target.isArray,
                "Longwatch runtime muzzle metadata direction array is unavailable.");
            target.arraySize = source.directions.Length;
            for (int directionIndex = 0; directionIndex < source.directions.Length; directionIndex++)
            {
                LongwatchMuzzleDirectionJson sourceDirection = source.directions[directionIndex];
                SerializedProperty targetDirection = target.GetArrayElementAtIndex(directionIndex);
                targetDirection.FindPropertyRelative("suffix").stringValue = sourceDirection.suffix;
                targetDirection.FindPropertyRelative("angleDegrees").intValue = sourceDirection.angleDegrees;
                targetDirection.FindPropertyRelative("supported").boolValue = sourceDirection.supported;
                SerializedProperty targetFrames =
                    targetDirection.FindPropertyRelative("frameOffsetsPixels");
                targetFrames.arraySize = sourceDirection.frames.Length;
                for (int frameIndex = 0; frameIndex < sourceDirection.frames.Length; frameIndex++)
                {
                    float[] offset = sourceDirection.frames[frameIndex].muzzleOffsetPixels;
                    targetFrames.GetArrayElementAtIndex(frameIndex).vector2Value =
                        new Vector2(offset[0], offset[1]);
                }
            }
        }

        private static void ValidateLongwatchMuzzleMetadataAsset(
            LongwatchMuzzleMetadata2D metadata)
        {
            Require(metadata != null && metadata.SchemaVersion == 1 &&
                metadata.GeneratorVersion == 1 && metadata.WeaponId == "longwatch_dmr" &&
                metadata.CellSizePixels == new Vector2Int(80, 96) &&
                metadata.PivotPixels == new Vector2Int(24, 8) &&
                metadata.GetSupportedPointCount() == 324,
                "Generated Longwatch runtime muzzle metadata asset is invalid.");

            LongwatchMuzzleState2D[] states =
            {
                LongwatchMuzzleState2D.Idle,
                LongwatchMuzzleState2D.Run,
                LongwatchMuzzleState2D.Backpedal,
                LongwatchMuzzleState2D.Crouch,
            };
            int[] frameCounts = { 2, 6, 4, 6 };
            for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                for (int directionIndex = 0;
                     directionIndex < LongwatchDirectionSuffixes.Length;
                     directionIndex++)
                {
                    LongwatchMuzzleDirection2D direction =
                        metadata.GetDirection(states[stateIndex], directionIndex);
                    bool expectedSupported =
                        states[stateIndex] != LongwatchMuzzleState2D.Crouch || directionIndex < 16;
                    Require(direction.Suffix == LongwatchDirectionSuffixes[directionIndex] &&
                        direction.AngleDegrees == LongwatchDirectionAngles[directionIndex] &&
                        direction.Supported == expectedSupported &&
                        direction.FrameCount == (expectedSupported ? frameCounts[stateIndex] : 0),
                        $"Runtime Longwatch muzzle metadata mismatch at " +
                        $"{states[stateIndex]}/{directionIndex}.");
                }
            }
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

            string[] stateNames =
            {
                "Idle", "Run", "Backpedal", "Jump", "Fall", "Land", "CrouchIdle", "CrouchMove",
                "CrouchBackpedal", "WallBrace",
            };
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
            WeaponDefinition2D longwatchDefinition,
            LongwatchMuzzleMetadata2D longwatchMuzzleMetadata,
            PhysicsMaterial2D physicsMaterial,
            RuntimeAnimatorController controller,
            PlayerJumpDustFx2D jumpDustPrefab,
            int groundLayer,
            int combatTargetLayer)
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
            List<Sprite> bodyCrouchFrames = LoadSpritesByFrameIndex(
                BodySpriteRoot + "/player_salvager_body_crouch.png");
            List<Sprite> longwatchIdleFrames = LoadLongwatchAimFrames(
                LongwatchIdleAimRoot, "idle", 2);
            List<Sprite> longwatchRunFrames = LoadLongwatchAimFrames(
                LongwatchRunAimRoot, "run", 6);
            List<Sprite> longwatchBackpedalFrames = LoadLongwatchAimFrames(
                LongwatchBackpedalAimRoot, "backpedal", 4);
            List<Sprite> longwatchCrouchFrames = LoadLongwatchAimFrames(
                LongwatchCrouchAimRoot, "crouch", 6);
            List<Sprite> longwatchMuzzleFlashFrames = LoadLongwatchMuzzleFlashFrames();
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "URP Sprite-Unlit-Default material is missing.");
            Require(bodyFrames.Count == 26 && armsFrames.Count == 26,
                "The player prefab requires all 26 unique Body and Unarmed Arms frames.");
            Require(bodyIdleFrames.Count == 2 && bodyRunFrames.Count == 6 &&
                bodyBackpedalFrames.Count == 4 && bodyCrouchFrames.Count == 6 &&
                longwatchIdleFrames.Count == 38 && longwatchRunFrames.Count == 114 &&
                longwatchBackpedalFrames.Count == 76 && longwatchCrouchFrames.Count == 114 &&
                longwatchMuzzleFlashFrames.Count == LongwatchMuzzleFlashPresenter2D.RequiredSpriteCount &&
                longwatchMuzzleMetadata != null,
                "The Longwatch presenter requires complete Idle, Run, Backpedal, and Crouch Body/armed frames.");

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
                SetString(input, "fireActionName", "Fire");
                PlayerGroundProbe2D probe = GetOrAddComponent<PlayerGroundProbe2D>(root);
                SetObjectReference(probe, "config", config);
                SetInteger(probe, "groundLayers", 1 << groundLayer);
                PlayerEnvironmentProbe2D environmentProbe = GetOrAddComponent<PlayerEnvironmentProbe2D>(root);
                SetObjectReference(environmentProbe, "config", config);
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
                    longwatchBackpedalFrames,
                    bodyCrouchFrames,
                    longwatchCrouchFrames);

                GameObject traceObject = GetOrCreateChild(root.transform, "Prototype Longwatch Shot Trace");
                LineRenderer traceRenderer = GetOrAddComponent<LineRenderer>(traceObject);
                ConfigureLineRenderer(traceRenderer, unlitMaterial, 2, true, 30);
                traceRenderer.SetPosition(0, Vector3.zero);
                traceRenderer.SetPosition(1, Vector3.zero);
                traceRenderer.enabled = false;
                PrototypeWeaponShotFeedback2D shotFeedback =
                    GetOrAddComponent<PrototypeWeaponShotFeedback2D>(traceObject);
                SetObjectReference(shotFeedback, "traceRenderer", traceRenderer);

                GameObject impactObject = GetOrCreateChild(traceObject.transform, "Impact");
                LineRenderer impactRenderer = GetOrAddComponent<LineRenderer>(impactObject);
                ConfigureLineRenderer(impactRenderer, unlitMaterial, 3, true, 31);
                impactRenderer.SetPosition(0, Vector3.zero);
                impactRenderer.SetPosition(1, Vector3.zero);
                impactRenderer.SetPosition(2, Vector3.zero);
                impactRenderer.enabled = false;
                SetObjectReference(shotFeedback, "impactRenderer", impactRenderer);

                PlayerWeaponController2D weaponController = GetOrAddComponent<PlayerWeaponController2D>(root);
                SetObjectReference(weaponController, "input", input);
                SetObjectReference(weaponController, "playerAim", playerAim);
                SetObjectReference(weaponController, "playerAnimator", presentation);
                SetObjectReference(weaponController, "playerMotor", motor);
                SetObjectReference(weaponController, "weaponDefinition", longwatchDefinition);
                SetInteger(weaponController, "hitLayers", (1 << groundLayer) | (1 << combatTargetLayer));
                SetObjectReference(weaponController, "shotFeedback", shotFeedback);

                LongwatchRecoilPresenter2D recoilPresenter =
                    GetOrAddComponent<LongwatchRecoilPresenter2D>(root);
                SetObjectReference(recoilPresenter, "weaponController", weaponController);
                SetObjectReference(recoilPresenter, "longwatchPresenter", longwatchPresenter);
                SetObjectReference(recoilPresenter, "armsWeaponTransform", armsVisual.transform);

                GameObject muzzleFlashObject =
                    GetOrCreateChild(armsVisual.transform, "LongwatchMuzzleFlash");
                SpriteRenderer muzzleFlashRenderer = GetOrAddComponent<SpriteRenderer>(muzzleFlashObject);
                muzzleFlashRenderer.sprite = longwatchMuzzleFlashFrames[0];
                muzzleFlashRenderer.sharedMaterial = unlitMaterial;
                muzzleFlashRenderer.sortingOrder = 12;
                muzzleFlashRenderer.flipX = false;
                muzzleFlashRenderer.enabled = false;
                LongwatchMuzzleFlashPresenter2D muzzleFlashPresenter =
                    GetOrAddComponent<LongwatchMuzzleFlashPresenter2D>(muzzleFlashObject);
                SetObjectReference(muzzleFlashPresenter, "weaponController", weaponController);
                SetObjectReference(muzzleFlashPresenter, "longwatchPresenter", longwatchPresenter);
                SetObjectReference(muzzleFlashPresenter, "metadata", longwatchMuzzleMetadata);
                SetObjectReference(muzzleFlashPresenter, "flashRenderer", muzzleFlashRenderer);
                SetObjectReferenceArray(muzzleFlashPresenter, "frames", longwatchMuzzleFlashFrames);

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
            string[] states = { "idle", "run", "backpedal", "jump", "fall", "land", "crouch", "wall_brace" };
            List<Sprite> frames = new List<Sprite>(26);
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

        private static List<Sprite> LoadLongwatchMuzzleFlashFrames()
        {
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(LongwatchMuzzleFlashPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            var frames = new List<Sprite>(LongwatchMuzzleFlashPresenter2D.RequiredSpriteCount);
            for (int variant = 0; variant < LongwatchMuzzleFlashMath.VariantCount; variant++)
            {
                for (int frame = 0; frame < LongwatchMuzzleFlashPresenter2D.FramesPerVariant; frame++)
                {
                    string name = "longwatch_dmr_muzzle_flash_v" + variant.ToString("00") +
                        "_f" + frame;
                    Require(sprites.TryGetValue(name, out Sprite sprite),
                        "Missing imported Longwatch muzzle-flash sprite: " + name);
                    frames.Add(sprite);
                }
            }

            Require(sprites.Count == LongwatchMuzzleFlashPresenter2D.RequiredSpriteCount,
                "Longwatch muzzle-flash sheet must import exactly 20 sprites.");
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
            IReadOnlyList<Sprite> longwatchBackpedalFrames,
            IReadOnlyList<Sprite> bodyCrouchFrames,
            IReadOnlyList<Sprite> longwatchCrouchFrames)
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

            SerializedProperty crouchBodyFrames = serialized.FindProperty("bodyCrouchFrames");
            crouchBodyFrames.arraySize = bodyCrouchFrames.Count;
            for (int index = 0; index < bodyCrouchFrames.Count; index++)
            {
                crouchBodyFrames.GetArrayElementAtIndex(index).objectReferenceValue = bodyCrouchFrames[index];
            }

            SerializedProperty crouchPoses = serialized.FindProperty("crouchAimPoses");
            crouchPoses.arraySize = LongwatchDirectionAngles.Length;
            for (int directionIndex = 0; directionIndex < LongwatchDirectionAngles.Length; directionIndex++)
            {
                SerializedProperty pose = crouchPoses.GetArrayElementAtIndex(directionIndex);
                pose.FindPropertyRelative("angleDegrees").intValue = LongwatchDirectionAngles[directionIndex];
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    pose.FindPropertyRelative("frame" + frameIndex).objectReferenceValue =
                        longwatchCrouchFrames[directionIndex * 6 + frameIndex];
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
            PlayerWeaponController2D weaponController = FindInScene<PlayerWeaponController2D>(scene);
            NativePixelPresentation nativePresentation = FindInScene<NativePixelPresentation>(scene);
            Require(playerAim != null,
                "MovementLab player is missing the generic aim component.");
            Require(nativePresentation != null,
                "MovementLab is missing its native-pixel presentation.");
            Require(weaponController != null,
                "MovementLab player is missing the Longwatch weapon controller.");
            SetObjectReference(playerAim, "nativePixelPresentation", nativePresentation);

            PixelCameraFollow2D cameraFollow = nativePresentation.WorldCamera.GetComponent<PixelCameraFollow2D>();
            Require(cameraFollow != null, "MovementLab world camera is missing pixel follow.");
            LongwatchCameraImpulse2D cameraImpulse =
                GetOrAddComponent<LongwatchCameraImpulse2D>(nativePresentation.WorldCamera.gameObject);
            SetObjectReference(cameraImpulse, "weaponController", weaponController);
            SetObjectReference(cameraImpulse, "cameraFollow", cameraFollow);
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
            int groundLayer,
            int combatTargetLayer)
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

            // RELEASE-CRITICAL: keep the runtime guard from the human-verified 047c49e fix.
            // Build-time collider baking is performed below and again by ReleaseCollisionBuildGuard.
            TilemapCompositeColliderInitializer2D collisionInitializer =
                collisionTilemap.gameObject.AddComponent<TilemapCompositeColliderInitializer2D>();

            Vector3Int[] courseCells = GetCourseCells().ToArray();
            TileBase[] visualTiles = Enumerable.Repeat<TileBase>(ruleTile, courseCells.Length).ToArray();
            TileBase[] collisionTiles = Enumerable.Repeat(collisionTile, courseCells.Length).ToArray();
            visualTilemap.SetTiles(courseCells, visualTiles);
            collisionTilemap.SetTiles(courseCells, collisionTiles);
            visualTilemap.RefreshAllTiles();
            collisionTilemap.RefreshAllTiles();

            // Do not save/export freshly edited Tilemap cells before the 2D collider pipeline has
            // consumed them. Editor Play Mode normally gets a LateUpdate that hides this problem;
            // a Player build can otherwise export stale/empty Composite geometry.
            BakeCompositeCollisionGeometry(collisionTilemap, collisionInitializer);

            // Tilemap.SetTiles changes native tile data. The collision Tilemap happens to receive
            // additional dirtiness through TilemapCollider2D; the visual Rule Tile map does not.
            // Explicitly dirty both maps and the scene so their serialized tile arrays survive saving.
            EditorUtility.SetDirty(visualTilemap);
            EditorUtility.SetDirty(visualTilemap.GetComponent<TilemapRenderer>());
            EditorUtility.SetDirty(collisionTilemap);
            EditorUtility.SetDirty(collisionTilemap.GetComponent<TilemapRenderer>());
            SynchronizePrecisionCrouchCeiling(scene, gridObject.transform, ruleTile, groundLayer);
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

            SynchronizeShootingRange(scene, root.transform, combatTargetLayer);
            SynchronizePrototypeEnemyEncounter(scene, root.transform, combatTargetLayer);
            CreateCamera(root.transform, player.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void SynchronizeMovementLabCourse(
            TileBase collisionTile,
            int groundLayer,
            int combatTargetLayer)
        {
            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Require(ruleTile != null, "Accepted M0 IndustrialSurface Rule Tile is missing.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Tilemap visualTilemap = FindTilemap(scene, "Industrial Surface - Visual");
            Tilemap collisionTilemap = FindTilemap(scene, "Ground Collision - Hidden");
            Require(visualTilemap != null && collisionTilemap != null,
                "MovementLab course Tilemaps are missing.");

            GameObject root = FindGameObject(scene, "RUSTLINE M1A - MOVEMENT LAB");
            Require(root != null, "MovementLab root is missing.");
            Vector3Int[] courseCells = GetCourseCells().ToArray();

            // Repair the Release collision contract on every deterministic rebuild instead of
            // assuming an older scene still has the required Rigidbody/Composite/initializer.
            bool changed = SynchronizeCompositeCollisionContract(collisionTilemap);
            changed |= SynchronizeTilemap(visualTilemap, courseCells, ruleTile);
            changed |= SynchronizeTilemap(collisionTilemap, courseCells, collisionTile);
            changed |= SynchronizePrecisionCrouchCeiling(
                scene, visualTilemap.transform.parent, ruleTile, groundLayer);

            // Always refresh/bake the collision geometry after opening the scene, even when the
            // authored cell set is already correct. This removes build output dependence on whether
            // the scene happened to receive an Editor LateUpdate before the Player build started.
            TilemapCompositeColliderInitializer2D collisionInitializer =
                collisionTilemap.GetComponent<TilemapCompositeColliderInitializer2D>();
            BakeCompositeCollisionGeometry(collisionTilemap, collisionInitializer);

            changed |= SynchronizeLabels(scene, root.transform);
            changed |= SynchronizeShootingRange(scene, root.transform, combatTargetLayer);
            changed |= SynchronizePrototypeEnemyEncounter(scene, root.transform, combatTargetLayer);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
        }

        private static bool SynchronizePrecisionCrouchCeiling(
            Scene scene,
            Transform gridParent,
            RuleTile ruleTile,
            int groundLayer)
        {
            bool changed = false;
            Tilemap tilemap = FindTilemap(scene, PrecisionCrouchCeilingName);
            if (tilemap == null)
            {
                tilemap = CreateTilemap(gridParent, PrecisionCrouchCeilingName, 0);
                changed = true;
            }

            Transform ceilingTransform = tilemap.transform;
            Vector3 desiredLocalPosition = new Vector3(0f, CrouchClearanceRaiseWorldUnits, 0f);
            if (ceilingTransform.parent != gridParent)
            {
                ceilingTransform.SetParent(gridParent, false);
                changed = true;
            }
            if (ceilingTransform.localPosition != desiredLocalPosition)
            {
                ceilingTransform.localPosition = desiredLocalPosition;
                changed = true;
            }
            if (ceilingTransform.localRotation != Quaternion.identity)
            {
                ceilingTransform.localRotation = Quaternion.identity;
                changed = true;
            }
            if (ceilingTransform.localScale != Vector3.one)
            {
                ceilingTransform.localScale = Vector3.one;
                changed = true;
            }
            if (tilemap.gameObject.layer != groundLayer)
            {
                tilemap.gameObject.layer = groundLayer;
                changed = true;
            }

            changed |= SynchronizeTilemap(tilemap, GetPrecisionCrouchCeilingCells().ToArray(), ruleTile);

            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(renderer != null && unlitMaterial != null,
                "Precision crouch ceiling requires its TilemapRenderer and unlit material.");
            if (!renderer.enabled) { renderer.enabled = true; changed = true; }
            if (renderer.sharedMaterial != unlitMaterial) { renderer.sharedMaterial = unlitMaterial; changed = true; }
            if (renderer.sortingOrder != 0) { renderer.sortingOrder = 0; changed = true; }

            Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(tilemap.gameObject);
            if (body.bodyType != RigidbodyType2D.Static) { body.bodyType = RigidbodyType2D.Static; changed = true; }

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(tilemap.gameObject);
            Vector2 desiredSize = new Vector2(PrecisionCrouchCeilingWidth, 1f);
            Vector2 desiredOffset = new Vector2(
                PrecisionCrouchCeilingLeft + PrecisionCrouchCeilingWidth * 0.5f,
                PrecisionCrouchCeilingCellY + 0.5f);
            if (collider.size != desiredSize) { collider.size = desiredSize; changed = true; }
            if (collider.offset != desiredOffset) { collider.offset = desiredOffset; changed = true; }
            if (collider.isTrigger) { collider.isTrigger = false; changed = true; }

            EditorUtility.SetDirty(tilemap);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(collider);
            return changed;
        }

        private static bool SynchronizeCompositeCollisionContract(Tilemap collisionTilemap)
        {
            bool changed = false;

            Rigidbody2D terrainBody = collisionTilemap.GetComponent<Rigidbody2D>();
            if (terrainBody == null)
            {
                terrainBody = collisionTilemap.gameObject.AddComponent<Rigidbody2D>();
                changed = true;
            }
            if (terrainBody.bodyType != RigidbodyType2D.Static)
            {
                terrainBody.bodyType = RigidbodyType2D.Static;
                changed = true;
            }

            TilemapCollider2D tilemapCollider = collisionTilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
            {
                tilemapCollider = collisionTilemap.gameObject.AddComponent<TilemapCollider2D>();
                changed = true;
            }
            if (!tilemapCollider.enabled)
            {
                tilemapCollider.enabled = true;
                changed = true;
            }
            if (tilemapCollider.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
                changed = true;
            }

            CompositeCollider2D composite = collisionTilemap.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = collisionTilemap.gameObject.AddComponent<CompositeCollider2D>();
                changed = true;
            }
            if (!composite.enabled)
            {
                composite.enabled = true;
                changed = true;
            }
            if (composite.geometryType != CompositeCollider2D.GeometryType.Polygons)
            {
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                changed = true;
            }

            TilemapCompositeColliderInitializer2D initializer =
                collisionTilemap.GetComponent<TilemapCompositeColliderInitializer2D>();
            if (initializer == null)
            {
                collisionTilemap.gameObject.AddComponent<TilemapCompositeColliderInitializer2D>();
                changed = true;
            }
            else if (!initializer.enabled)
            {
                initializer.enabled = true;
                changed = true;
            }

            return changed;
        }

        private static void BakeCompositeCollisionGeometry(
            Tilemap collisionTilemap,
            TilemapCompositeColliderInitializer2D initializer)
        {
            Require(collisionTilemap != null, "Collision Tilemap is missing.");
            TilemapCollider2D tilemapCollider = collisionTilemap.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = collisionTilemap.GetComponent<CompositeCollider2D>();
            Require(tilemapCollider != null && composite != null && initializer != null,
                "Release collision baking requires TilemapCollider2D, CompositeCollider2D, and initializer.");

            // Refreshing the authored Tile data makes the collider source current in the Editor.
            // ProcessTilemapChanges is the documented immediate path instead of waiting for
            // TilemapCollider2D's normal LateUpdate.
            collisionTilemap.RefreshAllTiles();
            initializer.EnsureGeometry();

            Require(composite.pathCount > 0 && composite.pointCount > 0,
                "Release collision bake produced empty Composite geometry. Refusing to save/build " +
                "a MovementLab that can make the Windows Player fall through the floor.");

            EditorUtility.SetDirty(tilemapCollider);
            EditorUtility.SetDirty(composite);
        }

        private static bool SynchronizeTilemap(
            Tilemap tilemap,
            IReadOnlyList<Vector3Int> desiredCells,
            TileBase desiredTile)
        {
            var desiredCellSet = new HashSet<Vector3Int>(desiredCells);
            bool changed = false;
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cell) && !desiredCellSet.Contains(cell))
                {
                    tilemap.SetTile(cell, null);
                    changed = true;
                }
            }

            for (int index = 0; index < desiredCells.Count; index++)
            {
                Vector3Int cell = desiredCells[index];
                if (tilemap.GetTile(cell) != desiredTile)
                {
                    tilemap.SetTile(cell, desiredTile);
                    changed = true;
                }
            }

            if (changed)
            {
                tilemap.RefreshAllTiles();
                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(tilemap.GetComponent<TilemapRenderer>());
            }

            return changed;
        }

        private static bool SynchronizeShootingRange(
            Scene scene,
            Transform parent,
            int combatTargetLayer)
        {
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "URP Sprite-Unlit-Default material is missing.");
            GameObject rangeRoot = FindGameObject(scene, "Longwatch Shooting Range");
            bool changed = false;
            if (rangeRoot == null)
            {
                rangeRoot = new GameObject("Longwatch Shooting Range");
                rangeRoot.transform.SetParent(parent, false);
                changed = true;
            }
            else if (rangeRoot.transform.parent != parent)
            {
                rangeRoot.transform.SetParent(parent, false);
                changed = true;
            }

            var retainedTargets = new HashSet<GameObject>();
            for (int index = 0; index < CombatTargets.Length; index++)
            {
                CombatTargetSpec spec = CombatTargets[index];
                Transform existing = rangeRoot.transform.Find(spec.Name);
                GameObject target = existing != null ? existing.gameObject : new GameObject(spec.Name);
                if (existing == null)
                {
                    target.transform.SetParent(rangeRoot.transform, false);
                    changed = true;
                }

                retainedTargets.Add(target);
                if (target.transform.position != spec.Position)
                {
                    target.transform.position = spec.Position;
                    changed = true;
                }
                if (target.layer != combatTargetLayer)
                {
                    target.layer = combatTargetLayer;
                    changed = true;
                }

                BoxCollider2D targetCollider = target.GetComponent<BoxCollider2D>();
                if (targetCollider == null)
                {
                    targetCollider = target.AddComponent<BoxCollider2D>();
                    changed = true;
                }
                if (!targetCollider.isTrigger)
                {
                    targetCollider.isTrigger = true;
                    changed = true;
                }
                if (targetCollider.size != new Vector2(1f, 1.5f))
                {
                    targetCollider.size = new Vector2(1f, 1.5f);
                    changed = true;
                }

                LineRenderer targetRenderer = target.GetComponent<LineRenderer>();
                if (targetRenderer == null)
                {
                    targetRenderer = target.AddComponent<LineRenderer>();
                    changed = true;
                }
                changed |= ConfigureLineRenderer(targetRenderer, unlitMaterial, 5, false, 20);
                changed |= SetLinePosition(targetRenderer, 0, new Vector3(-0.5f, -0.75f));
                changed |= SetLinePosition(targetRenderer, 1, new Vector3(0.5f, -0.75f));
                changed |= SetLinePosition(targetRenderer, 2, new Vector3(0.5f, 0.75f));
                changed |= SetLinePosition(targetRenderer, 3, new Vector3(-0.5f, 0.75f));
                changed |= SetLinePosition(targetRenderer, 4, new Vector3(-0.5f, -0.75f));
                Color targetColor = RustlinePalette.GetColor(14);
                if (targetRenderer.startColor != targetColor || targetRenderer.endColor != targetColor)
                {
                    targetRenderer.startColor = targetColor;
                    targetRenderer.endColor = targetColor;
                    changed = true;
                }
                if (!targetRenderer.enabled)
                {
                    targetRenderer.enabled = true;
                    changed = true;
                }

                DiagnosticCombatTarget2D receiver = target.GetComponent<DiagnosticCombatTarget2D>();
                if (receiver == null)
                {
                    receiver = target.AddComponent<DiagnosticCombatTarget2D>();
                    changed = true;
                }
                SetObjectReference(receiver, "targetRenderer", targetRenderer);
            }

            for (int index = rangeRoot.transform.childCount - 1; index >= 0; index--)
            {
                GameObject child = rangeRoot.transform.GetChild(index).gameObject;
                if (!retainedTargets.Contains(child))
                {
                    UnityEngine.Object.DestroyImmediate(child);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool SynchronizePrototypeEnemyEncounter(
            Scene scene,
            Transform parent,
            int combatTargetLayer)
        {
            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "URP Sprite-Unlit-Default material is missing.");

            GameObject encounterObject = FindGameObject(scene, "First Combat Entity Encounter");
            bool changed = false;
            if (encounterObject == null)
            {
                encounterObject = new GameObject("First Combat Entity Encounter");
                encounterObject.transform.SetParent(parent, false);
                changed = true;
            }
            else if (encounterObject.transform.parent != parent)
            {
                encounterObject.transform.SetParent(parent, false);
                changed = true;
            }

            MovementLabPrototypeEnemyEncounter2D encounter =
                encounterObject.GetComponent<MovementLabPrototypeEnemyEncounter2D>();
            if (encounter == null)
            {
                encounter = encounterObject.AddComponent<MovementLabPrototypeEnemyEncounter2D>();
                changed = true;
            }

            Transform enemyTransform = encounterObject.transform.Find("Prototype Ground Enemy");
            GameObject enemyObject;
            if (enemyTransform == null)
            {
                enemyObject = new GameObject("Prototype Ground Enemy");
                enemyObject.transform.SetParent(encounterObject.transform, false);
                changed = true;
            }
            else
            {
                enemyObject = enemyTransform.gameObject;
            }

            if (enemyObject.transform.position != PrototypeEnemySpawnPosition)
            {
                enemyObject.transform.position = PrototypeEnemySpawnPosition;
                changed = true;
            }

            Rigidbody2D enemyBody = enemyObject.GetComponent<Rigidbody2D>();
            if (enemyBody == null)
            {
                enemyBody = enemyObject.AddComponent<Rigidbody2D>();
                changed = true;
            }
            if (enemyBody.bodyType != RigidbodyType2D.Kinematic)
            {
                enemyBody.bodyType = RigidbodyType2D.Kinematic;
                changed = true;
            }
            if (!Mathf.Approximately(enemyBody.gravityScale, 0f))
            {
                enemyBody.gravityScale = 0f;
                changed = true;
            }
            if (enemyBody.constraints != RigidbodyConstraints2D.FreezeRotation)
            {
                enemyBody.constraints = RigidbodyConstraints2D.FreezeRotation;
                changed = true;
            }

            CombatHealth2D health = enemyObject.GetComponent<CombatHealth2D>();
            if (health == null)
            {
                health = enemyObject.AddComponent<CombatHealth2D>();
                changed = true;
            }
            if (health.MaximumHealth != 100)
            {
                SetInteger(health, "maximumHealth", 100);
                changed = true;
            }

            PrototypeGroundEnemy2D enemy = enemyObject.GetComponent<PrototypeGroundEnemy2D>();
            if (enemy == null)
            {
                enemy = enemyObject.AddComponent<PrototypeGroundEnemy2D>();
                changed = true;
            }

            Transform visualTransform = enemyObject.transform.Find("Programmer Art Presentation");
            GameObject visualObject;
            if (visualTransform == null)
            {
                visualObject = new GameObject("Programmer Art Presentation");
                visualObject.transform.SetParent(enemyObject.transform, false);
                changed = true;
            }
            else
            {
                visualObject = visualTransform.gameObject;
            }
            if (visualObject.transform.localPosition != Vector3.zero)
            {
                visualObject.transform.localPosition = Vector3.zero;
                changed = true;
            }

            LineRenderer silhouette = visualObject.GetComponent<LineRenderer>();
            if (silhouette == null)
            {
                silhouette = visualObject.AddComponent<LineRenderer>();
                changed = true;
            }
            changed |= ConfigureLineRenderer(silhouette, unlitMaterial, 7, false, 20);
            changed |= SetLinePosition(silhouette, 0, new Vector3(-0.6f, 0f));
            changed |= SetLinePosition(silhouette, 1, new Vector3(-0.6f, 1.8f));
            changed |= SetLinePosition(silhouette, 2, new Vector3(-0.3f, 2.5f));
            changed |= SetLinePosition(silhouette, 3, new Vector3(0.3f, 2.5f));
            changed |= SetLinePosition(silhouette, 4, new Vector3(0.6f, 1.8f));
            changed |= SetLinePosition(silhouette, 5, new Vector3(0.6f, 0f));
            changed |= SetLinePosition(silhouette, 6, new Vector3(-0.6f, 0f));
            Color aliveColor = RustlinePalette.GetColor(19);
            if (silhouette.startColor != aliveColor || silhouette.endColor != aliveColor)
            {
                silhouette.startColor = aliveColor;
                silhouette.endColor = aliveColor;
                changed = true;
            }
            if (!silhouette.enabled)
            {
                silhouette.enabled = true;
                changed = true;
            }

            PrototypeGroundEnemyPresenter2D presenter =
                visualObject.GetComponent<PrototypeGroundEnemyPresenter2D>();
            if (presenter == null)
            {
                presenter = visualObject.AddComponent<PrototypeGroundEnemyPresenter2D>();
                changed = true;
            }

            Transform hitboxTransform = enemyObject.transform.Find("Hitbox");
            GameObject hitboxObject;
            if (hitboxTransform == null)
            {
                hitboxObject = new GameObject("Hitbox");
                hitboxObject.transform.SetParent(enemyObject.transform, false);
                changed = true;
            }
            else
            {
                hitboxObject = hitboxTransform.gameObject;
            }
            Vector3 hitboxLocalPosition = new Vector3(0f, 1.25f, 0f);
            if (hitboxObject.transform.localPosition != hitboxLocalPosition)
            {
                hitboxObject.transform.localPosition = hitboxLocalPosition;
                changed = true;
            }
            if (hitboxObject.layer != combatTargetLayer)
            {
                hitboxObject.layer = combatTargetLayer;
                changed = true;
            }

            BoxCollider2D hitCollider = hitboxObject.GetComponent<BoxCollider2D>();
            if (hitCollider == null)
            {
                hitCollider = hitboxObject.AddComponent<BoxCollider2D>();
                changed = true;
            }
            if (!hitCollider.isTrigger)
            {
                hitCollider.isTrigger = true;
                changed = true;
            }
            Vector2 hitboxSize = new Vector2(1.2f, 2.5f);
            if (hitCollider.size != hitboxSize)
            {
                hitCollider.size = hitboxSize;
                changed = true;
            }

            WeaponHitbox2D weaponHitbox = hitboxObject.GetComponent<WeaponHitbox2D>();
            if (weaponHitbox == null)
            {
                weaponHitbox = hitboxObject.AddComponent<WeaponHitbox2D>();
                changed = true;
            }

            SetObjectReference(weaponHitbox, "health", health);
            SetObjectReference(presenter, "health", health);
            SetObjectReference(presenter, "silhouetteRenderer", silhouette);
            SetObjectReference(enemy, "health", health);
            SetObjectReference(enemy, "body", enemyBody);
            SetObjectReference(enemy, "weaponHitCollider", hitCollider);
            SetFloat(enemy, "patrolSpeed", PrototypeGroundEnemy2D.DefaultPatrolSpeed);
            SetFloat(enemy, "patrolHalfDistance", PrototypeGroundEnemy2D.DefaultPatrolHalfDistance);
            SetInteger(enemy, "initialDirection", 1);
            SetFloat(enemy, "hitPauseDuration", PrototypeGroundEnemy2D.DefaultHitPauseDuration);
            SetObjectReference(encounter, "enemy", enemy);
            SetFloat(encounter, "resetDelay", MovementLabPrototypeEnemyEncounter2D.DefaultResetDelay);

            return changed;
        }

        private static bool ConfigureLineRenderer(
            LineRenderer renderer,
            Material material,
            int positionCount,
            bool useWorldSpace,
            int sortingOrder)
        {
            bool changed = false;
            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                changed = true;
            }
            if (renderer.positionCount != positionCount)
            {
                renderer.positionCount = positionCount;
                changed = true;
            }
            if (renderer.useWorldSpace != useWorldSpace)
            {
                renderer.useWorldSpace = useWorldSpace;
                changed = true;
            }
            if (!Mathf.Approximately(renderer.startWidth, PrototypeWeaponShotFeedback2D.TraceWidth) ||
                !Mathf.Approximately(renderer.endWidth, PrototypeWeaponShotFeedback2D.TraceWidth))
            {
                renderer.startWidth = PrototypeWeaponShotFeedback2D.TraceWidth;
                renderer.endWidth = PrototypeWeaponShotFeedback2D.TraceWidth;
                changed = true;
            }
            if (renderer.sortingOrder != sortingOrder)
            {
                renderer.sortingOrder = sortingOrder;
                changed = true;
            }
            if (renderer.loop)
            {
                renderer.loop = false;
                changed = true;
            }

            return changed;
        }

        private static bool SetLinePosition(LineRenderer renderer, int index, Vector3 position)
        {
            if (renderer.GetPosition(index) == position)
            {
                return false;
            }

            renderer.SetPosition(index, position);
            return true;
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
            for (int index = 0; index < DiagnosticLabels.Length; index++)
            {
                DiagnosticLabel label = DiagnosticLabels[index];
                CreateLabel(labels.transform, label.Text, label.Position, label.Size, label.Color);
            }
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

        private static bool SynchronizeLabels(Scene scene, Transform parent)
        {
            GameObject labelsObject = FindGameObject(scene, "Diagnostic Labels");
            bool changed = false;
            if (labelsObject == null)
            {
                labelsObject = new GameObject("Diagnostic Labels");
                labelsObject.transform.SetParent(parent, false);
                changed = true;
            }
            else if (labelsObject.transform.parent != parent)
            {
                labelsObject.transform.SetParent(parent, false);
                changed = true;
            }

            Transform labels = labelsObject.transform;
            var retainedLabels = new HashSet<Transform>();
            for (int specIndex = 0; specIndex < DiagnosticLabels.Length; specIndex++)
            {
                DiagnosticLabel spec = DiagnosticLabels[specIndex];
                Transform existing = null;
                for (int childIndex = 0; childIndex < labels.childCount; childIndex++)
                {
                    Transform child = labels.GetChild(childIndex);
                    if (child.name == spec.Text && !retainedLabels.Contains(child))
                    {
                        existing = child;
                        break;
                    }
                }

                if (existing == null)
                {
                    CreateLabel(labels, spec.Text, spec.Position, spec.Size, spec.Color);
                    existing = labels.GetChild(labels.childCount - 1);
                    changed = true;
                }

                retainedLabels.Add(existing);
                changed |= SynchronizeLabel(existing, spec);
            }

            for (int childIndex = labels.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = labels.GetChild(childIndex);
                if (!retainedLabels.Contains(child))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool SynchronizeLabel(Transform labelTransform, DiagnosticLabel spec)
        {
            bool changed = false;
            if (labelTransform.position != spec.Position)
            {
                labelTransform.position = spec.Position;
                changed = true;
            }

            TextMesh textMesh = labelTransform.GetComponent<TextMesh>();
            if (textMesh == null)
            {
                textMesh = labelTransform.gameObject.AddComponent<TextMesh>();
                changed = true;
            }

            if (textMesh.text != spec.Text)
            {
                textMesh.text = spec.Text;
                changed = true;
            }
            if (textMesh.anchor != TextAnchor.MiddleCenter)
            {
                textMesh.anchor = TextAnchor.MiddleCenter;
                changed = true;
            }
            if (textMesh.alignment != TextAlignment.Center)
            {
                textMesh.alignment = TextAlignment.Center;
                changed = true;
            }
            if (!Mathf.Approximately(textMesh.characterSize, spec.Size))
            {
                textMesh.characterSize = spec.Size;
                changed = true;
            }
            if (textMesh.fontSize != 32)
            {
                textMesh.fontSize = 32;
                changed = true;
            }
            if (textMesh.color != spec.Color)
            {
                textMesh.color = spec.Color;
                changed = true;
            }

            MeshRenderer renderer = labelTransform.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = labelTransform.gameObject.AddComponent<MeshRenderer>();
                changed = true;
            }
            Material fontMaterial = textMesh.font != null ? textMesh.font.material : null;
            if (renderer.sharedMaterial != fontMaterial)
            {
                renderer.sharedMaterial = fontMaterial;
                changed = true;
            }
            if (renderer.sortingOrder != 100)
            {
                renderer.sortingOrder = 100;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(labelTransform);
                EditorUtility.SetDirty(textMesh);
                EditorUtility.SetDirty(renderer);
            }

            return changed;
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
            Require(Mathf.Approximately(config.MaxCrouchGroundSpeed, 3f) &&
                config.StandingColliderSize == new Vector2(1.05f, 2.75f) &&
                config.StandingColliderOffset == new Vector2(0f, 1.375f) &&
                config.CrouchColliderSize == new Vector2(1.05f, 2.375f) &&
                config.CrouchColliderOffset == new Vector2(0f, 1.1875f),
                "Player combat-crouch tuning or foot-anchor collider contract changed.");
            Require(Mathf.Approximately(config.WallBraceMaxFallSpeed, 4f) &&
                Mathf.Approximately(config.WallKickHorizontalSpeed, 8f) &&
                Mathf.Approximately(config.WallKickVerticalSpeed, 11.5f) &&
                Mathf.Approximately(config.WallKickLockDuration, 0.12f),
                "Player wall-brace/wall-kick tuning changed from 4/8/11.5/0.12.");

            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputActionMap playerMap = input?.FindActionMap("Player", false);
            Require(input != null && input.actionMaps.Count == 1 && playerMap != null,
                "Input asset must contain only the focused Player action map for M1A.");
            Require(playerMap.actions.Count == 5 && playerMap.FindAction("Move", false) != null &&
                playerMap.FindAction("Jump", false) != null &&
                playerMap.FindAction("Crouch", false) != null &&
                playerMap.FindAction("Fire", false) != null &&
                playerMap.FindAction("PointerPosition", false) != null,
                "Player input must contain Move, Jump, Crouch, Fire, and PointerPosition.");
            Require(playerMap.FindAction("Move").bindings.Any(binding => binding.path == "<Gamepad>/dpad"),
                "Move must support the gamepad D-pad.");
            Require(playerMap.FindAction("Jump").bindings.Any(binding => binding.path == "<Keyboard>/space") &&
                playerMap.FindAction("Jump").bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"),
                "Jump bindings are incomplete.");
            Require(playerMap.FindAction("Crouch").bindings.Any(binding => binding.path == "<Keyboard>/s") &&
                playerMap.FindAction("Crouch").bindings.Any(binding => binding.path == "<Keyboard>/downArrow") &&
                playerMap.FindAction("Crouch").bindings.Any(binding => binding.path == "<Gamepad>/dpad/down") &&
                playerMap.FindAction("Crouch").bindings.Any(binding => binding.path == "<Gamepad>/leftStick/down"),
                "Crouch bindings are incomplete.");
            InputAction fire = playerMap.FindAction("Fire");
            Require(fire.type == InputActionType.Button && fire.expectedControlType == "Button" &&
                fire.bindings.Count == 1 && fire.bindings[0].path == "<Mouse>/leftButton" &&
                fire.bindings[0].interactions == "Press",
                "Fire must be a Button action bound only to mouse-left with Press semantics.");
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
            AnimatorController gameplayController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Require(gameplayController != null, "Player gameplay Animator controller is missing.");
            AnimatorState[] gameplayStates = gameplayController.layers[0].stateMachine.states
                .Select(child => child.state)
                .ToArray();
            string[] expectedGameplayStateNames =
            {
                "Idle", "Run", "Backpedal", "Jump", "Fall", "Land", "CrouchIdle", "CrouchMove",
                "CrouchBackpedal", "WallBrace",
            };
            Require(gameplayStates.Length == expectedGameplayStateNames.Length,
                "Player gameplay Animator must contain exactly the required locomotion states.");
            foreach (string stateName in expectedGameplayStateNames)
            {
                AnimatorState state = gameplayStates.FirstOrDefault(candidate => candidate.name == stateName);
                AnimationClip expectedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    BodyAnimationRoot + "/Player_Body_" + stateName + ".anim");
                Require(state != null && expectedClip != null && state.motion == expectedClip,
                    "Gameplay Animator state must use its matching Body clip: " + stateName + ".");
            }
            PlayerAim2D playerAim = prefab.GetComponent<PlayerAim2D>();
            PlayerAnimator2D playerAnimator = prefab.GetComponent<PlayerAnimator2D>();
            Require(prefab.GetComponent<PlayerInputReader>() != null && prefab.GetComponent<PlayerGroundProbe2D>() != null &&
                prefab.GetComponent<PlayerEnvironmentProbe2D>() != null &&
                prefab.GetComponent<PlayerMotor2D>() != null && prefab.GetComponent<PlayerAnimator2D>() != null &&
                playerAim != null,
                "Player prefab movement components are incomplete.");
            Require(playerAnimator.PlayerAim == playerAim,
                "Player animator must consume the generic aim-facing source.");
            WeaponDefinition2D longwatchDefinition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LongwatchDefinitionPath);
            string weaponReason = "asset is missing";
            Require(longwatchDefinition != null && longwatchDefinition.IsSane(out weaponReason),
                "Longwatch definition is missing or invalid: " + weaponReason);
            Require(longwatchDefinition.WeaponId == "longwatch_dmr" &&
                longwatchDefinition.DisplayName == "Longwatch DMR" &&
                longwatchDefinition.FireMode == WeaponFireMode2D.SemiAutomatic &&
                Mathf.Approximately(longwatchDefinition.ShotInterval, 0.25f) &&
                Mathf.Approximately(longwatchDefinition.Range, 80f) &&
                longwatchDefinition.Damage == 40,
                "Longwatch prototype definition changed from semi-auto / 0.25 s / 80 u / 40 damage.");
            LongwatchMuzzleMetadata2D longwatchMuzzleMetadata =
                AssetDatabase.LoadAssetAtPath<LongwatchMuzzleMetadata2D>(
                    LongwatchMuzzleMetadataAssetPath);
            ValidateLongwatchMuzzleMetadataAsset(longwatchMuzzleMetadata);
            PlayerWeaponController2D weaponController = prefab.GetComponent<PlayerWeaponController2D>();
            PrototypeWeaponShotFeedback2D shotFeedback =
                prefab.GetComponentInChildren<PrototypeWeaponShotFeedback2D>(true);
            Require(weaponController != null && weaponController.WeaponDefinition == longwatchDefinition &&
                weaponController.HitLayers == ((1 << 6) | (1 << 7)) &&
                weaponController.ShotFeedback == shotFeedback && shotFeedback != null &&
                shotFeedback.TraceRenderer != null &&
                shotFeedback.ImpactRenderer != null &&
                Mathf.Approximately(shotFeedback.TraceRenderer.startWidth, 1f / 16f) &&
                Mathf.Approximately(shotFeedback.TraceRenderer.endWidth, 1f / 16f),
                "Player prefab Longwatch gameplay or prototype trace wiring is incomplete.");
            LongwatchRecoilPresenter2D recoilPresenter = prefab.GetComponent<LongwatchRecoilPresenter2D>();
            Require(recoilPresenter != null && recoilPresenter.WeaponController == weaponController &&
                recoilPresenter.LongwatchPresenter == prefab.GetComponent<PlayerLongwatchAimPresenter2D>() &&
                recoilPresenter.ArmsWeaponTransform == prefab.transform.Find(
                    "Visual - 48x64 Full Cell/ArmsWeaponSpriteRenderer"),
                "Player prefab Longwatch presentation recoil wiring is incomplete.");
            PlayerUnarmedArmsPresenter2D armsPresenter = prefab.GetComponent<PlayerUnarmedArmsPresenter2D>();
            Require(armsPresenter != null && armsPresenter.MappingCount == 26 && armsPresenter.OwnsRenderer,
                "Player prefab must contain the complete active unarmed arms presenter.");
            PlayerLongwatchAimPresenter2D longwatchPresenter =
                prefab.GetComponent<PlayerLongwatchAimPresenter2D>();
            PlayerJumpPresentation2D jumpPresentation = prefab.GetComponent<PlayerJumpPresentation2D>();
            Transform visual = prefab.transform.Find("Visual - 48x64 Full Cell");
            Require(visual != null && Vector3.Distance(visual.localPosition, new Vector3(0f, -0.25f, 0f)) < 0.0001f,
                "Player visual child must be lowered exactly 4 source pixels (-0.25 Unity units).");
            Transform bodyVisual = visual.Find("BodySpriteRenderer");
            Transform armsVisual = visual.Find("ArmsWeaponSpriteRenderer");
            Transform muzzleFlashVisual = armsVisual?.Find("LongwatchMuzzleFlash");
            Transform aimOrigin = visual.Find("AimOrigin");
            SpriteRenderer bodyRenderer = bodyVisual?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = armsVisual?.GetComponent<SpriteRenderer>();
            SpriteRenderer muzzleFlashRenderer = muzzleFlashVisual?.GetComponent<SpriteRenderer>();
            LongwatchMuzzleFlashPresenter2D muzzleFlashPresenter =
                muzzleFlashVisual?.GetComponent<LongwatchMuzzleFlashPresenter2D>();
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
            Require(muzzleFlashVisual != null && muzzleFlashVisual.parent == armsVisual &&
                muzzleFlashVisual.localPosition == Vector3.zero &&
                muzzleFlashVisual.localRotation == Quaternion.identity &&
                muzzleFlashVisual.localScale == Vector3.one &&
                muzzleFlashRenderer != null && muzzleFlashRenderer.sortingOrder == 12 &&
                !muzzleFlashRenderer.enabled && !muzzleFlashRenderer.flipX &&
                muzzleFlashRenderer.sharedMaterial == unlitMaterial &&
                muzzleFlashPresenter != null &&
                muzzleFlashPresenter.WeaponController == weaponController &&
                muzzleFlashPresenter.LongwatchPresenter == longwatchPresenter &&
                muzzleFlashPresenter.Metadata == longwatchMuzzleMetadata &&
                muzzleFlashPresenter.FlashRenderer == muzzleFlashRenderer &&
                muzzleFlashPresenter.SpriteCount == LongwatchMuzzleFlashPresenter2D.RequiredSpriteCount,
                "Player prefab Longwatch muzzle-flash hierarchy or wiring is incomplete.");
            for (int variant = 0; variant < LongwatchMuzzleFlashMath.VariantCount; variant++)
            {
                for (int frame = 0; frame < LongwatchMuzzleFlashPresenter2D.FramesPerVariant; frame++)
                {
                    int spriteIndex = variant * LongwatchMuzzleFlashPresenter2D.FramesPerVariant + frame;
                    Sprite sprite = muzzleFlashPresenter.GetSprite(spriteIndex);
                    Require(sprite != null && sprite.name ==
                        "longwatch_dmr_muzzle_flash_v" + variant.ToString("00") + "_f" + frame,
                        "Player prefab Longwatch muzzle-flash sprite order mismatch at " + spriteIndex + ".");
                }
            }
            Require(longwatchPresenter != null && longwatchPresenter.PlayerAim == playerAim &&
                longwatchPresenter.PlayerAnimator == prefab.GetComponent<PlayerAnimator2D>() &&
                longwatchPresenter.UnarmedPresenter == armsPresenter &&
                longwatchPresenter.BodySpriteRenderer == bodyRenderer &&
                longwatchPresenter.ArmsWeaponSpriteRenderer == armsRenderer &&
                longwatchPresenter.BodyIdleFrameCount == 2 && longwatchPresenter.IdleAimPoseCount == 19 &&
                longwatchPresenter.BodyRunFrameCount == 6 && longwatchPresenter.RunAimPoseCount == 19 &&
                longwatchPresenter.BodyBackpedalFrameCount == 4 &&
                longwatchPresenter.BackpedalAimPoseCount == 19 &&
                longwatchPresenter.BodyCrouchFrameCount == 6 &&
                longwatchPresenter.CrouchAimPoseCount == 19,
                "Player prefab Longwatch Idle/Run/Backpedal/Crouch presenter wiring is incomplete.");
            for (int frameIndex = 0; frameIndex < 6; frameIndex++)
            {
                Sprite bodyCrouchFrame = longwatchPresenter.GetBodyCrouchFrame(frameIndex);
                Require(bodyCrouchFrame != null &&
                    bodyCrouchFrame.name == "player_salvager_body_crouch_" + frameIndex,
                    "Longwatch presenter Body crouch frame mismatch at frame " + frameIndex + ".");
            }
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

                LongwatchCrouchAimPose crouchPose = longwatchPresenter.GetCrouchAimPose(index);
                Require(crouchPose.AngleDegrees == LongwatchDirectionAngles[index],
                    "Longwatch Crouch presenter angle mismatch at direction " + index + ".");
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    Sprite frame = crouchPose.GetFrame(frameIndex);
                    Require(frame != null &&
                        frame.name.EndsWith("_" + LongwatchDirectionSuffixes[index] + "_" + frameIndex),
                        $"Longwatch Crouch presenter pose mismatch at direction {index}, frame {frameIndex}.");
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

            ValidateScene(reopenMovementLabFromDisk, config);
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

        private static void ValidateScene(bool reopenFromDisk, PlayerMovementConfig config)
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

                Tilemap precisionCeiling = FindTilemap(scene, PrecisionCrouchCeilingName);
                Require(precisionCeiling != null &&
                    precisionCeiling.gameObject.layer == 6 &&
                    precisionCeiling.transform.localPosition ==
                        new Vector3(0f, CrouchClearanceRaiseWorldUnits, 0f),
                    "MovementLab precision crouch ceiling is missing or has the wrong 10 px vertical offset.");
                Require(CountOccupiedCells(precisionCeiling) == PrecisionCrouchCeilingWidth,
                    "Precision crouch ceiling must contain exactly nine visual cells.");
                foreach (Vector3Int cell in GetPrecisionCrouchCeilingCells())
                {
                    Require(precisionCeiling.GetTile(cell) == ruleTile,
                        "Precision crouch ceiling is missing its visual RuleTile at " + cell + ".");
                }
                BoxCollider2D precisionCeilingCollider = precisionCeiling.GetComponent<BoxCollider2D>();
                Rigidbody2D precisionCeilingBody = precisionCeiling.GetComponent<Rigidbody2D>();
                Require(precisionCeilingCollider != null && !precisionCeilingCollider.isTrigger &&
                    precisionCeilingCollider.size == new Vector2(PrecisionCrouchCeilingWidth, 1f) &&
                    precisionCeilingCollider.offset == new Vector2(
                        PrecisionCrouchCeilingLeft + PrecisionCrouchCeilingWidth * 0.5f,
                        PrecisionCrouchCeilingCellY + 0.5f) &&
                    precisionCeilingBody != null &&
                    precisionCeilingBody.bodyType == RigidbodyType2D.Static,
                    "Precision crouch ceiling must use its exact static sub-cell BoxCollider2D contract.");
                float crouchOpeningHeight =
                    PrecisionCrouchCeilingCellY + CrouchClearanceRaiseWorldUnits;
                Require(Mathf.Approximately(crouchOpeningHeight, 2.625f) &&
                    config.CrouchColliderSize.y < crouchOpeningHeight &&
                    config.StandingColliderSize.y > crouchOpeningHeight,
                    "The crouch-only opening must be exactly 42 px: passable crouched, not standable.");

                TilemapCollider2D collider = FindInScene<TilemapCollider2D>(scene);
                CompositeCollider2D composite = collider?.GetComponent<CompositeCollider2D>();
                Rigidbody2D terrainBody = collider?.GetComponent<Rigidbody2D>();
                TilemapCompositeColliderInitializer2D collisionInitializer =
                    collider?.GetComponent<TilemapCompositeColliderInitializer2D>();
                Require(collider != null && collider.enabled &&
                    collider.compositeOperation == Collider2D.CompositeOperation.Merge &&
                    composite != null && composite.enabled &&
                    composite.geometryType == CompositeCollider2D.GeometryType.Polygons &&
                    terrainBody != null && terrainBody.bodyType == RigidbodyType2D.Static,
                    "MovementLab Release collision contract requires enabled TilemapCollider2D Merge -> " +
                    "CompositeCollider2D Polygons on a static Rigidbody2D.");
                Require(collisionInitializer != null && collisionInitializer.enabled,
                    "MovementLab Release collision contract requires the enabled " +
                    "TilemapCompositeColliderInitializer2D startup guard. See docs/RELEASE_COLLISION.md.");

                // Validation must inspect actual generated geometry, not only serialized component
                // wiring. This is the exact gap that can pass Editor configuration checks yet fail
                // in a Windows Player.
                BakeCompositeCollisionGeometry(collisionTilemap, collisionInitializer);
                Require(composite.pathCount > 0 && composite.pointCount > 0,
                    "MovementLab Composite collision geometry is empty after deterministic bake.");

                Require(LayerMask.NameToLayer("Ground") == 6 &&
                    LayerMask.NameToLayer("CombatTarget") == 7,
                    "Ground and CombatTarget must remain on layers 6 and 7 respectively.");
                GameObject rangeRoot = FindGameObject(scene, "Longwatch Shooting Range");
                Require(rangeRoot != null && rangeRoot.transform.childCount == CombatTargets.Length,
                    "MovementLab Longwatch shooting range is missing or contains unexpected targets.");
                for (int index = 0; index < CombatTargets.Length; index++)
                {
                    CombatTargetSpec spec = CombatTargets[index];
                    Transform targetTransform = rangeRoot.transform.Find(spec.Name);
                    BoxCollider2D targetCollider = targetTransform?.GetComponent<BoxCollider2D>();
                    DiagnosticCombatTarget2D target = targetTransform?.GetComponent<DiagnosticCombatTarget2D>();
                    Require(targetTransform != null && targetTransform.gameObject.layer == 7 &&
                        targetTransform.position == spec.Position && targetCollider != null &&
                        targetCollider.isTrigger && targetCollider.size == new Vector2(1f, 1.5f) &&
                        target != null && target.TargetRenderer != null,
                        "MovementLab CombatTarget setup mismatch for " + spec.Name + ".");
                }

                MovementLabPrototypeEnemyEncounter2D encounter =
                    FindInScene<MovementLabPrototypeEnemyEncounter2D>(scene);
                PrototypeGroundEnemy2D enemy = encounter?.Enemy;
                CombatHealth2D enemyHealth = enemy?.Health;
                Rigidbody2D enemyBody = enemy?.Body;
                Collider2D enemyHitCollider = enemy?.WeaponHitCollider;
                WeaponHitbox2D enemyHitbox = enemyHitCollider?.GetComponent<WeaponHitbox2D>();
                PrototypeGroundEnemyPresenter2D enemyPresenter =
                    enemy?.GetComponentInChildren<PrototypeGroundEnemyPresenter2D>(true);
                Require(encounter != null && enemy != null &&
                    enemy.transform.position == PrototypeEnemySpawnPosition &&
                    enemyHealth != null && enemyHealth.MaximumHealth == 100 &&
                    enemyBody != null && enemyBody.bodyType == RigidbodyType2D.Kinematic &&
                    Mathf.Approximately(enemyBody.gravityScale, 0f) &&
                    enemyHitCollider != null && enemyHitCollider.gameObject.layer == 7 &&
                    enemyHitCollider.isTrigger && enemyHitCollider.enabled &&
                    enemyHitbox != null && enemyHitbox.gameObject == enemyHitCollider.gameObject &&
                    enemyHitbox.transform.parent == enemy.transform &&
                    enemyHitbox.Health == enemyHealth &&
                    enemyPresenter != null && enemyPresenter.Health == enemyHealth &&
                    Mathf.Approximately(enemy.PatrolSpeed, PrototypeGroundEnemy2D.DefaultPatrolSpeed) &&
                    Mathf.Approximately(
                        enemy.PatrolHalfDistance,
                        PrototypeGroundEnemy2D.DefaultPatrolHalfDistance) &&
                    enemy.InitialDirection == 1 &&
                    Mathf.Approximately(
                        encounter.ResetDelay,
                        MovementLabPrototypeEnemyEncounter2D.DefaultResetDelay),
                    "MovementLab first combat entity hierarchy, tuning, or explicit hitbox routing is invalid.");

                Camera worldCamera = FindCamera(scene, "World Camera - Native Pixel Follow");
                Camera driverCamera = FindCamera(scene, "Native Pixel Driver Camera");
                Require(GetComponentsInScene<Camera>(scene).Count == 2,
                    "MovementLab must contain only the world camera and the native-pixel RenderGraph driver camera.");
                Require(worldCamera != null && worldCamera.orthographic && !worldCamera.allowHDR &&
                    !worldCamera.allowMSAA && worldCamera.CompareTag("MainCamera"),
                    "MovementLab logical world camera configuration is invalid.");
                PixelCameraFollow2D cameraFollow = worldCamera.GetComponent<PixelCameraFollow2D>();
                LongwatchCameraImpulse2D cameraImpulse = worldCamera.GetComponent<LongwatchCameraImpulse2D>();
                PlayerWeaponController2D weaponController = FindInScene<PlayerWeaponController2D>(scene);
                Require(cameraFollow != null && cameraImpulse != null &&
                    cameraImpulse.CameraFollow == cameraFollow &&
                    cameraImpulse.WeaponController == weaponController,
                    "MovementLab camera follow or Longwatch camera impulse wiring is missing.");
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

        private static IEnumerable<Vector3Int> GetPrecisionCrouchCeilingCells()
        {
            for (int x = PrecisionCrouchCeilingLeft;
                 x < PrecisionCrouchCeilingLeft + PrecisionCrouchCeilingWidth;
                 x++)
            {
                yield return new Vector3Int(x, PrecisionCrouchCeilingCellY, 0);
            }
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

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null, $"Serialized property {propertyName} is missing on {target.GetType().Name}.");
            property.stringValue = value;
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
