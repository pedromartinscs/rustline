using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Deterministically builds the narrow M0 art-integration deliverables.
    /// Safe to rerun from Tools/Rustline/Rebuild M0 Art Showcase.
    /// </summary>
    public static class RustlineM0ArtSetup
    {
        private const string PlayerRoot = "Assets/Art/Characters/Player";
        private const string BodySpriteRoot = PlayerRoot + "/Sprites/Body";
        private const string UnarmedArmsSpriteRoot = PlayerRoot + "/Sprites/Arms/Unarmed";
        private const string LongwatchIdleAimRoot =
            PlayerRoot + "/Sprites/Arms/Armed/longwatch_dmr/Aim/Idle";
        private const string LongwatchRunAimRoot =
            PlayerRoot + "/Sprites/Arms/Armed/longwatch_dmr/Aim/Run";
        private const string LongwatchBackpedalAimRoot =
            PlayerRoot + "/Sprites/Arms/Armed/longwatch_dmr/Aim/Backpedal";
        private const string MovementEffectsRoot = "Assets/Art/Effects/Movement";
        private const string JumpDustPath = MovementEffectsRoot + "/player_jump_dust.png";
        private const string AtlasPath = "Assets/Art/Environment/Tiles/industrial_surface.png";
        private const string AnimationRoot = PlayerRoot + "/Animations";
        private const string BodyAnimationRoot = AnimationRoot + "/Body";
        private const string TileAssetRoot = "Assets/Art/Environment/Tiles/Generated";
        private const string RuleTilePath = TileAssetRoot + "/IndustrialSurfaceRuleTile.asset";
        private const string ScenePath = "Assets/Scenes/ArtShowcase.unity";
        private const float JumpClipSampleRate = 50f;

        private static readonly float[] JumpTakeoffKeyframeTimes = { 0f, 0.1f, 0.26f };

        private static readonly SheetSpec[] PlayerSheets =
        {
            new SheetSpec(BodySpriteRoot, "player_salvager_body_idle", 2),
            new SheetSpec(BodySpriteRoot, "player_salvager_body_run", 6),
            new SheetSpec(BodySpriteRoot, "player_salvager_body_backpedal", 4),
            new SheetSpec(BodySpriteRoot, "player_salvager_body_jump", 3),
            new SheetSpec(BodySpriteRoot, "player_salvager_body_fall", 1),
            new SheetSpec(BodySpriteRoot, "player_salvager_body_land", 2),
            new SheetSpec(UnarmedArmsSpriteRoot, "player_salvager_arms_idle", 2),
            new SheetSpec(UnarmedArmsSpriteRoot, "player_salvager_arms_run", 6),
            new SheetSpec(UnarmedArmsSpriteRoot, "player_salvager_arms_backpedal", 4),
            new SheetSpec(UnarmedArmsSpriteRoot, "player_salvager_arms_jump", 3),
            new SheetSpec(UnarmedArmsSpriteRoot, "player_salvager_arms_fall", 1),
            new SheetSpec(UnarmedArmsSpriteRoot, "player_salvager_arms_land", 2),
        };

        private static readonly LongwatchDirectionSpec[] LongwatchDirections =
        {
            new LongwatchDirectionSpec("p90", 90),
            new LongwatchDirectionSpec("p80", 80),
            new LongwatchDirectionSpec("p70", 70),
            new LongwatchDirectionSpec("p60", 60),
            new LongwatchDirectionSpec("p50", 50),
            new LongwatchDirectionSpec("p40", 40),
            new LongwatchDirectionSpec("p30", 30),
            new LongwatchDirectionSpec("p20", 20),
            new LongwatchDirectionSpec("p10", 10),
            new LongwatchDirectionSpec("0", 0),
            new LongwatchDirectionSpec("m10", -10),
            new LongwatchDirectionSpec("m20", -20),
            new LongwatchDirectionSpec("m30", -30),
            new LongwatchDirectionSpec("m40", -40),
            new LongwatchDirectionSpec("m50", -50),
            new LongwatchDirectionSpec("m60", -60),
            new LongwatchDirectionSpec("m70", -70),
            new LongwatchDirectionSpec("m80", -80),
            new LongwatchDirectionSpec("m90", -90),
        };

        // Bits are N=1, E=2, S=4, W=8. Slot order is the canonical documented order.
        private static readonly int[] CanonicalConnectivityMasks =
        {
            0, 1, 2, 4, 8, 3, 6, 12, 9, 5, 10, 7, 14, 13, 11, 15,
        };

        private static readonly string[] AtlasSlotSuffixes =
        {
            "none", "n", "e", "s", "w", "n_e", "e_s", "s_w",
            "w_n", "n_s", "e_w", "n_e_s", "e_s_w", "s_w_n", "w_n_e", "n_e_s_w",
            "thin_isolated", "thin_left_cap", "thin_middle_a", "thin_middle_b",
            "thin_right_cap", "thin_damaged_left", "thin_damaged_middle", "thin_damaged_right",
            "top_variant_b", "top_variant_c", "left_wall_variant_b", "right_wall_variant_b",
            "ceiling_variant_b", "interior_variant_b", "interior_variant_c", "interior_reinforced",
            "top_light_damage", "top_heavy_damage", "left_wall_damaged", "right_wall_damaged",
            "ceiling_damaged", "interior_rusted", "interior_dented", "interior_heavy_corrosion",
            "hazard_block", "reinforced_top", "reinforced_wall", "reinforced_ceiling",
            "structural_support", "structural_junction", "reserved_46", "reserved_47",
        };

        private sealed class SheetSpec
        {
            internal SheetSpec(string assetRoot, string fileName, int frameCount)
            {
                AssetRoot = assetRoot;
                FileName = fileName;
                FrameCount = frameCount;
            }

            internal string AssetRoot { get; }
            internal string FileName { get; }
            internal int FrameCount { get; }
            internal string AssetPath => AssetRoot + "/" + FileName + ".png";
        }

        private sealed class LongwatchDirectionSpec
        {
            internal LongwatchDirectionSpec(string suffix, int angleDegrees)
            {
                Suffix = suffix;
                AngleDegrees = angleDegrees;
            }

            internal string Suffix { get; }
            internal int AngleDegrees { get; }
            internal string IdleFileName => "player_salvager_longwatch_dmr_idle_aim_" + Suffix;
            internal string IdleAssetPath => LongwatchIdleAimRoot + "/" + IdleFileName + ".png";
            internal string RunFileName => "player_salvager_longwatch_dmr_run_aim_" + Suffix;
            internal string RunAssetPath => LongwatchRunAimRoot + "/" + RunFileName + ".png";
            internal string BackpedalFileName => "player_salvager_longwatch_dmr_backpedal_aim_" + Suffix;
            internal string BackpedalAssetPath => LongwatchBackpedalAimRoot + "/" + BackpedalFileName + ".png";
        }

        private sealed class PreviewAsset
        {
            internal PreviewAsset(
                string label,
                AnimationClip clip,
                RuntimeAnimatorController controller,
                IReadOnlyList<Sprite> bodyFrames,
                IReadOnlyList<Sprite> armsFrames)
            {
                Label = label;
                Clip = clip;
                Controller = controller;
                BodyFrames = bodyFrames;
                ArmsFrames = armsFrames;
            }

            internal string Label { get; }
            internal AnimationClip Clip { get; }
            internal RuntimeAnimatorController Controller { get; }
            internal IReadOnlyList<Sprite> BodyFrames { get; }
            internal IReadOnlyList<Sprite> ArmsFrames { get; }
        }

        [MenuItem("Tools/Rustline/Rebuild M0 Art Showcase")]
        public static void RebuildFromMenu()
        {
            BuildAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline M0 Art Showcase",
                "Pixel-art imports, preview clips, Rule Tile, and ArtShowcase scene were rebuilt and validated.",
                "OK");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildAndValidate();
                Debug.Log("RUSTLINE_M0_VALIDATION_OK");
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

        [MenuItem("Tools/Rustline/Validate M0 Art Integration")]
        public static void ValidateFromMenu()
        {
            ValidateAllOrThrow();
            EditorUtility.DisplayDialog("Rustline M0 Art Integration", "All deterministic M0 checks passed.", "OK");
        }

        private static void BuildAndValidate()
        {
            EnsureFolder(AnimationRoot);
            EnsureFolder(BodyAnimationRoot);
            EnsureFolder(MovementEffectsRoot);
            EnsureFolder(TileAssetRoot);

            ConfigurePlayerSheets();
            ConfigureLongwatchAimSheets();
            ConfigureJumpDust();
            ConfigureEnvironmentAtlas();
            MoveLegacyBodyAnimationClips();

            Dictionary<string, PreviewAsset> previews = CreateAnimationPreviews();
            RuleTile ruleTile = CreateRuleTile();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateShowcaseScene(previews, ruleTile);
            }
            else
            {
                MigrateShowcasePlayerSpecimens(previews);
            }
            PutShowcaseInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateAllOrThrow();
        }

        private static void ConfigurePlayerSheets()
        {
            foreach (SheetSpec sheet in PlayerSheets)
            {
                ConfigureFixedGrid(
                    sheet.AssetPath,
                    48,
                    64,
                    sheet.FrameCount,
                    index => sheet.FileName + "_" + index,
                    new Vector2(0.5f, 0f),
                    logicalRowsRunTopToBottom: false);
            }
        }

        private static void ConfigureEnvironmentAtlas()
        {
            ConfigureFixedGrid(
                AtlasPath,
                16,
                16,
                48,
                AtlasSpriteName,
                new Vector2(0.5f, 0.5f),
                logicalRowsRunTopToBottom: true);
        }

        private static void ConfigureLongwatchAimSheets()
        {
            foreach (LongwatchDirectionSpec direction in LongwatchDirections)
            {
                ConfigureFixedGrid(
                    direction.IdleAssetPath,
                    80,
                    96,
                    2,
                    index => direction.IdleFileName + "_" + index,
                    new Vector2(0.3f, 8f / 96f),
                    logicalRowsRunTopToBottom: false);
                ConfigureFixedGrid(
                    direction.RunAssetPath,
                    80,
                    96,
                    6,
                    index => direction.RunFileName + "_" + index,
                    new Vector2(0.3f, 8f / 96f),
                    logicalRowsRunTopToBottom: false);
                ConfigureFixedGrid(
                    direction.BackpedalAssetPath,
                    80,
                    96,
                    4,
                    index => direction.BackpedalFileName + "_" + index,
                    new Vector2(0.3f, 8f / 96f),
                    logicalRowsRunTopToBottom: false);
            }
        }

        private static void ConfigureJumpDust()
        {
            ConfigureFixedGrid(
                JumpDustPath,
                48,
                64,
                3,
                index => "player_jump_dust_" + index,
                new Vector2(0.5f, 0f),
                logicalRowsRunTopToBottom: false);
        }

        private static void MoveLegacyBodyAnimationClips()
        {
            string[] states = { "Idle", "Run", "Backpedal", "Jump", "Fall", "Land" };
            foreach (string state in states)
            {
                string sourcePath = AnimationRoot + "/Player_" + state + ".anim";
                string destinationPath = BodyAnimationRoot + "/Player_Body_" + state + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
                if (clip == null && AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath) != null)
                {
                    string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
                    Require(string.IsNullOrEmpty(error), "Could not preserve the body clip GUID while moving " + sourcePath + ": " + error);
                    clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
                }

                if (clip != null)
                {
                    clip.name = "Player_Body_" + state;
                    EditorUtility.SetDirty(clip);
                }
            }
        }

        private static void ConfigureFixedGrid(
            string path,
            int cellWidth,
            int cellHeight,
            int spriteCount,
            Func<int, string> spriteName,
            Vector2 pivot,
            bool logicalRowsRunTopToBottom)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, "Texture importer not found: " + path);

            RustlinePixelArtPostprocessor.ApplyBaseline(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture != null, "Texture failed to import: " + path);
            Require(texture.width % cellWidth == 0 && texture.height % cellHeight == 0,
                $"{path} dimensions {texture.width}x{texture.height} do not align to {cellWidth}x{cellHeight} cells.");
            int columns = texture.width / cellWidth;
            int rows = texture.height / cellHeight;
            Require(columns * rows == spriteCount,
                $"{path} must be exactly {spriteCount} cells, but imported as {texture.width}x{texture.height}.");

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            Require(provider != null, "Sprite data provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .GroupBy(rect => rect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);

            SpriteRect[] rects = new SpriteRect[spriteCount];
            for (int index = 0; index < spriteCount; index++)
            {
                int column = index % columns;
                int logicalRow = index / columns;
                int unityRow = logicalRowsRunTopToBottom ? rows - 1 - logicalRow : logicalRow;
                string name = spriteName(index);

                rects[index] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(column * cellWidth, unityRow * cellHeight, cellWidth, cellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(name, out GUID existingId) ? existingId : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            Require(nameProvider != null, "Sprite name/file-ID provider unavailable: " + path);
            nameProvider.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static Dictionary<string, PreviewAsset> CreateAnimationPreviews()
        {
            Dictionary<string, PreviewAsset> previews = new Dictionary<string, PreviewAsset>();
            AddPreview(previews, "Idle", "idle", 0.5f, true);
            AddPreview(previews, "Run", "run", 10f, true);
            AddPreview(previews, "Backpedal", "backpedal", 7f, true);
            AddPreview(previews, "Jump", "jump", JumpClipSampleRate, false, JumpTakeoffKeyframeTimes);
            AddPreview(previews, "Fall", "fall", 1f, false);
            AddPreview(previews, "Land", "land", 8f, true);
            return previews;
        }

        private static void AddPreview(
            IDictionary<string, PreviewAsset> previews,
            string label,
            string stateId,
            float frameRate,
            bool loop,
            IReadOnlyList<float> keyframeTimes = null)
        {
            string bodySheetPath = BodySpriteRoot + "/player_salvager_body_" + stateId + ".png";
            string armsSheetPath = UnarmedArmsSpriteRoot + "/player_salvager_arms_" + stateId + ".png";
            List<Sprite> bodyFrames = LoadSprites(bodySheetPath)
                .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                .ToList();
            List<Sprite> armsFrames = LoadSprites(armsSheetPath)
                .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                .ToList();
            Require(bodyFrames.Count > 0, "No frames found for " + bodySheetPath);
            Require(bodyFrames.Count == armsFrames.Count, label + " body/arms frame counts differ.");
            Require(keyframeTimes == null || keyframeTimes.Count == bodyFrames.Count,
                label + " explicit keyframe timing count must match its sprite count.");

            string clipName = "Player_Body_" + label;
            string clipPath = BodyAnimationRoot + "/" + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.ClearCurves();
            clip.frameRate = frameRate;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;

            List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
            for (int index = 0; index < bodyFrames.Count; index++)
            {
                keyframes.Add(new ObjectReferenceKeyframe
                {
                    time = keyframeTimes != null ? keyframeTimes[index] : index / frameRate,
                    value = bodyFrames[index],
                });
            }

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite",
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            string controllerPath = AnimationRoot + "/Preview_" + label + ".controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ChildAnimatorState[] existingStates = stateMachine.states;
            AnimatorState state;
            if (existingStates.Length > 0)
            {
                state = existingStates[0].state;
                for (int index = 1; index < existingStates.Length; index++)
                {
                    stateMachine.RemoveState(existingStates[index].state);
                }
            }
            else
            {
                state = stateMachine.AddState(clipName);
            }

            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);

            previews.Add(label, new PreviewAsset(label, clip, controller, bodyFrames, armsFrames));
        }

        private static RuleTile CreateRuleTile()
        {
            List<Sprite> sprites = LoadSprites(AtlasPath).ToList();
            Require(sprites.Count == 48, "Industrial surface atlas must expose all 48 fixed slots.");
            Dictionary<string, Sprite> byName = sprites.ToDictionary(sprite => sprite.name);

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            if (ruleTile == null)
            {
                ruleTile = ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(ruleTile, RuleTilePath);
            }

            ruleTile.m_DefaultSprite = byName[AtlasSpriteName(15)];
            ruleTile.m_DefaultGameObject = null;
            ruleTile.m_DefaultColliderType = Tile.ColliderType.None;
            ruleTile.m_TilingRules.Clear();

            Vector3Int[] positions =
            {
                Vector3Int.up,
                Vector3Int.right,
                Vector3Int.down,
                Vector3Int.left,
            };

            for (int slot = 0; slot < 16; slot++)
            {
                int mask = CanonicalConnectivityMasks[slot];
                RuleTile.TilingRule rule = new RuleTile.TilingRule
                {
                    m_Id = 1000 + slot,
                    m_NeighborPositions = new List<Vector3Int>(positions),
                    m_Neighbors = new List<int>
                    {
                        (mask & 1) != 0 ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis,
                        (mask & 2) != 0 ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis,
                        (mask & 4) != 0 ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis,
                        (mask & 8) != 0 ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis,
                    },
                    m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
                    m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                    m_ColliderType = Tile.ColliderType.None,
                    m_Sprites = new[] { byName[AtlasSpriteName(slot)] },
                };
                ruleTile.m_TilingRules.Add(rule);
            }

            ruleTile.UpdateNeighborPositions();
            EditorUtility.SetDirty(ruleTile);
            return ruleTile;
        }

        private static void CreateShowcaseScene(IReadOnlyDictionary<string, PreviewAsset> previews, RuleTile ruleTile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("RUSTLINE M0 - ART SHOWCASE");

            CreateCamera(root.transform);
            CreateGlobalLight(root.transform);

            GameObject labelsRoot = new GameObject("Diagnostic Labels");
            labelsRoot.transform.SetParent(root.transform, false);
            CreateLabel(labelsRoot.transform, "RUSTLINE M0 - ART SHOWCASE", new Vector3(0f, 12.55f, -0.2f), 0.24f, new Color32(32, 237, 229, 255));
            CreateLabel(labelsRoot.transform, "PRESS PLAY TO PREVIEW ANIMATIONS  |  16 PPU  |  POINT FILTERED", new Vector3(0f, 11.9f, -0.2f), 0.12f, new Color32(201, 187, 177, 255));

            Grid grid = CreateGrid(root.transform);
            Tilemap playerGround = CreateTilemap(grid.transform, "Player Scale Ground");
            FillRectangle(playerGround, ruleTile, -22, 4, 44, 2);

            CreatePlayerSpecimens(root.transform, labelsRoot.transform, previews);
            CreateCanonicalSlotDisplay(root.transform, labelsRoot.transform);

            Tilemap structures = CreateTilemap(grid.transform, "Rule Tile Structure Tests");
            CreateStructureTests(structures, ruleTile, labelsRoot.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void MigrateShowcasePlayerSpecimens(IReadOnlyDictionary<string, PreviewAsset> previews)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (PreviewAsset preview in previews.Values)
            {
                GameObject specimen = FindGameObject(scene, "Player_" + preview.Label + "_Specimen");
                if (specimen == null)
                {
                    GameObject specimensRoot = FindGameObject(scene, "Player Animation Specimens - 48x64 Cells");
                    GameObject labelsRoot = FindGameObject(scene, "Diagnostic Labels");
                    Require(specimensRoot != null && labelsRoot != null,
                        "ArtShowcase player specimen roots are missing.");
                    specimen = new GameObject("Player_" + preview.Label + "_Specimen");
                    specimen.transform.SetParent(specimensRoot.transform, false);
                    specimen.transform.position = new Vector3(20f, 6f, 0f);
                    CreateLabel(labelsRoot.transform, preview.Label.ToUpperInvariant(),
                        new Vector3(20f, 10.65f, -0.2f), 0.16f, new Color32(253, 208, 69, 255));
                }

                SpriteRenderer legacyRenderer = specimen.GetComponent<SpriteRenderer>();
                Animator legacyAnimator = specimen.GetComponent<Animator>();
                if (legacyRenderer != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacyRenderer);
                }
                if (legacyAnimator != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacyAnimator);
                }

                GameObject bodyObject = GetOrCreateChild(specimen.transform, "BodySpriteRenderer");
                SpriteRenderer bodyRenderer = GetOrAddComponent<SpriteRenderer>(bodyObject);
                bodyRenderer.sprite = preview.BodyFrames[0];
                bodyRenderer.sortingOrder = 5;
                Animator animator = GetOrAddComponent<Animator>(bodyObject);
                animator.runtimeAnimatorController = preview.Controller;

                GameObject armsObject = GetOrCreateChild(specimen.transform, "ArmsWeaponSpriteRenderer");
                SpriteRenderer armsRenderer = GetOrAddComponent<SpriteRenderer>(armsObject);
                armsRenderer.sprite = preview.ArmsFrames[0];
                armsRenderer.sortingOrder = 6;

                PlayerUnarmedArmsPresenter2D presenter = GetOrAddComponent<PlayerUnarmedArmsPresenter2D>(specimen);
                ConfigureLayeredPresenter(presenter, bodyRenderer, armsRenderer, preview.BodyFrames, preview.ArmsFrames);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
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

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static void CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 13.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(1, 2, 11, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;

            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = 16;
            pixelPerfect.refResolutionX = 768;
            pixelPerfect.refResolutionY = 432;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;

            SerializedObject serializedPixelPerfect = new SerializedObject(pixelPerfect);
            serializedPixelPerfect.FindProperty("m_FilterMode").enumValueIndex = (int)PixelPerfectCamera.PixelPerfectFilterMode.Point;
            serializedPixelPerfect.ApplyModifiedPropertiesWithoutUndo();
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

        private static Grid CreateGrid(Transform parent)
        {
            GameObject gridObject = new GameObject("Environment Grid - 1x1 Cells");
            gridObject.transform.SetParent(parent, false);
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            grid.cellGap = Vector3.zero;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            return grid;
        }

        private static Tilemap CreateTilemap(Transform parent, string name)
        {
            GameObject tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent, false);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
            return tilemap;
        }

        private static void CreatePlayerSpecimens(
            Transform parent,
            Transform labelsRoot,
            IReadOnlyDictionary<string, PreviewAsset> previews)
        {
            GameObject specimensRoot = new GameObject("Player Animation Specimens - 48x64 Cells");
            specimensRoot.transform.SetParent(parent, false);
            string[] order = { "Idle", "Run", "Backpedal", "Jump", "Fall", "Land" };
            float[] xPositions = { -20f, -12f, -4f, 4f, 12f, 20f };

            for (int index = 0; index < order.Length; index++)
            {
                PreviewAsset preview = previews[order[index]];
                GameObject specimen = new GameObject("Player_" + preview.Label + "_Specimen");
                specimen.transform.SetParent(specimensRoot.transform, false);
                specimen.transform.position = new Vector3(xPositions[index], 6f, 0f);

                GameObject bodyObject = new GameObject("BodySpriteRenderer");
                bodyObject.transform.SetParent(specimen.transform, false);
                SpriteRenderer bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
                bodyRenderer.sprite = preview.BodyFrames[0];
                bodyRenderer.sortingOrder = 5;

                Animator animator = bodyObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = preview.Controller;

                GameObject armsObject = new GameObject("ArmsWeaponSpriteRenderer");
                armsObject.transform.SetParent(specimen.transform, false);
                SpriteRenderer armsRenderer = armsObject.AddComponent<SpriteRenderer>();
                armsRenderer.sprite = preview.ArmsFrames[0];
                armsRenderer.sortingOrder = 6;

                PlayerUnarmedArmsPresenter2D presenter = specimen.AddComponent<PlayerUnarmedArmsPresenter2D>();
                ConfigureLayeredPresenter(presenter, bodyRenderer, armsRenderer, preview.BodyFrames, preview.ArmsFrames);

                CreateLabel(
                    labelsRoot,
                    preview.Label.ToUpperInvariant(),
                    new Vector3(xPositions[index], 10.65f, -0.2f),
                    0.16f,
                    new Color32(253, 208, 69, 255));
            }
        }

        internal static void ConfigureLayeredPresenter(
            PlayerUnarmedArmsPresenter2D presenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            IReadOnlyList<Sprite> bodyFrames,
            IReadOnlyList<Sprite> armsFrames)
        {
            Require(bodyFrames.Count == armsFrames.Count, "Body and arms mapping counts must match.");
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("bodySpriteRenderer").objectReferenceValue = bodyRenderer;
            serialized.FindProperty("armsWeaponSpriteRenderer").objectReferenceValue = armsRenderer;
            SerializedProperty mappings = serialized.FindProperty("frameMappings");
            mappings.arraySize = bodyFrames.Count;
            for (int index = 0; index < bodyFrames.Count; index++)
            {
                SerializedProperty mapping = mappings.GetArrayElementAtIndex(index);
                mapping.FindPropertyRelative("bodySprite").objectReferenceValue = bodyFrames[index];
                mapping.FindPropertyRelative("armsSprite").objectReferenceValue = armsFrames[index];
            }

            serialized.FindProperty("ownsRenderer").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCanonicalSlotDisplay(Transform parent, Transform labelsRoot)
        {
            GameObject slotsRoot = new GameObject("Canonical Slots 00-15 - Explicit Sprites");
            slotsRoot.transform.SetParent(parent, false);
            Dictionary<string, Sprite> sprites = LoadSprites(AtlasPath).ToDictionary(sprite => sprite.name);

            CreateLabel(labelsRoot, "CANONICAL TILE SLOTS 00-15", new Vector3(-15.6f, 3.15f, -0.2f), 0.16f, new Color32(32, 237, 229, 255));

            for (int slot = 0; slot < 16; slot++)
            {
                int column = slot % 8;
                int row = slot / 8;
                float x = -20.4f + column * 1.35f;
                float y = 1.45f - row * 2.15f;

                GameObject specimen = new GameObject(AtlasSpriteName(slot));
                specimen.transform.SetParent(slotsRoot.transform, false);
                specimen.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer renderer = specimen.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[AtlasSpriteName(slot)];
                renderer.sortingOrder = 3;

                string ruleLabel = slot.ToString("00") + " " + AtlasSlotSuffixes[slot].ToUpperInvariant().Replace('_', '+');
                CreateLabel(labelsRoot, ruleLabel, new Vector3(x, y - 0.72f, -0.2f), 0.075f, new Color32(201, 187, 177, 255));
            }
        }

        private static void CreateStructureTests(Tilemap tilemap, RuleTile ruleTile, Transform labelsRoot)
        {
            CreateLabel(labelsRoot, "RULE TILE ADJACENCY TESTS", new Vector3(5.3f, 3.15f, -0.2f), 0.16f, new Color32(32, 237, 229, 255));

            tilemap.SetTile(new Vector3Int(-8, 1, 0), ruleTile);
            for (int x = -5; x <= -1; x++)
            {
                tilemap.SetTile(new Vector3Int(x, 1, 0), ruleTile);
            }

            for (int y = 1; y >= -2; y--)
            {
                tilemap.SetTile(new Vector3Int(2, y, 0), ruleTile);
            }

            Vector3Int[] lShape =
            {
                new Vector3Int(5, 1, 0),
                new Vector3Int(5, 0, 0),
                new Vector3Int(5, -1, 0),
                new Vector3Int(6, -1, 0),
                new Vector3Int(7, -1, 0),
            };
            foreach (Vector3Int position in lShape)
            {
                tilemap.SetTile(position, ruleTile);
            }

            FillRectangle(tilemap, ruleTile, 10, -3, 5, 5);
            tilemap.RefreshAllTiles();

            Color32 labelColor = new Color32(201, 187, 177, 255);
            CreateLabel(labelsRoot, "ISOLATED", new Vector3(-7.5f, -0.1f, -0.2f), 0.09f, labelColor);
            CreateLabel(labelsRoot, "HORIZONTAL STRIP", new Vector3(-3f, -0.1f, -0.2f), 0.09f, labelColor);
            CreateLabel(labelsRoot, "VERTICAL COLUMN", new Vector3(2f, -3.0f, -0.2f), 0.09f, labelColor);
            CreateLabel(labelsRoot, "L-SHAPE", new Vector3(6f, -2.15f, -0.2f), 0.09f, labelColor);
            CreateLabel(labelsRoot, "12 TOP / FLOOR", new Vector3(12f, 2.2f, -0.2f), 0.09f, new Color32(253, 208, 69, 255));
            CreateLabel(labelsRoot, "11 LEFT WALL", new Vector3(8.7f, -0.5f, -0.2f), 0.08f, labelColor);
            CreateLabel(labelsRoot, "15 INTERIOR FILL", new Vector3(18.5f, -1.25f, -0.2f), 0.08f, labelColor);
            CreateLabel(labelsRoot, "13 RIGHT WALL", new Vector3(15.4f, -0.5f, -0.2f), 0.08f, labelColor);
            CreateLabel(labelsRoot, "14 CEILING / UNDERSIDE", new Vector3(12f, -4.15f, -0.2f), 0.09f, new Color32(253, 208, 69, 255));
            CreateLabel(labelsRoot, "SOLID 5x5 RECTANGLE", new Vector3(12f, -4.65f, -0.2f), 0.09f, labelColor);
        }

        private static void FillRectangle(Tilemap tilemap, TileBase tile, int xMin, int yMin, int width, int height)
        {
            for (int x = xMin; x < xMin + width; x++)
            {
                for (int y = yMin; y < yMin + height; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            tilemap.RefreshAllTiles();
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position, float characterSize, Color color)
        {
            GameObject labelObject = new GameObject("Label - " + text);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 64;
            textMesh.color = color;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = textMesh.font.material;
            renderer.sortingOrder = 100;
        }

        private static void PutShowcaseInBuildSettings()
        {
            const string movementLabPath = "Assets/Scenes/MovementLab.unity";
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            int movementLabIndex = scenes.FindIndex(scene =>
                string.Equals(scene.path, movementLabPath, StringComparison.OrdinalIgnoreCase));
            scenes.Insert(movementLabIndex >= 0 ? movementLabIndex + 1 : 0,
                new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static void ValidateAllOrThrow()
        {
            foreach (SheetSpec sheet in PlayerSheets)
            {
                ValidateImporter(sheet.AssetPath);
                ValidateSpriteGrid(sheet.AssetPath, 48, 64, sheet.FrameCount, false, index => sheet.FileName + "_" + index, new Vector2(24f, 0f));
                ValidateSourcePixels(sheet.AssetPath);
            }

            ValidateLongwatchIdleAimSheets();
            ValidateLongwatchRunAimSheets();
            ValidateLongwatchBackpedalAimSheets();

            ValidateImporter(JumpDustPath);
            ValidateSpriteGrid(
                JumpDustPath,
                48,
                64,
                3,
                false,
                index => "player_jump_dust_" + index,
                new Vector2(24f, 0f));
            ValidateSourcePixels(JumpDustPath);
            Texture2D jumpDust = AssetDatabase.LoadAssetAtPath<Texture2D>(JumpDustPath);
            Require(jumpDust != null && jumpDust.width == 144 && jumpDust.height == 64,
                "Jump dust must remain exactly 144x64 (three 48x64 full cells).");

            string[] layeredStates = { "idle", "run", "backpedal", "jump", "fall", "land" };
            foreach (string state in layeredStates)
            {
                string bodyPath = BodySpriteRoot + "/player_salvager_body_" + state + ".png";
                string armsPath = UnarmedArmsSpriteRoot + "/player_salvager_arms_" + state + ".png";
                Texture2D body = AssetDatabase.LoadAssetAtPath<Texture2D>(bodyPath);
                Texture2D arms = AssetDatabase.LoadAssetAtPath<Texture2D>(armsPath);
                Require(body != null && arms != null && body.width == arms.width && body.height == arms.height,
                    state + " Body and Arms sheets must use identical cell geometry.");
                Require(LoadSprites(bodyPath).Count() == LoadSprites(armsPath).Count(),
                    state + " Body and Arms sprite counts must match.");
            }

            ValidateImporter(AtlasPath);
            ValidateSpriteGrid(AtlasPath, 16, 16, 48, true, AtlasSpriteName, new Vector2(8f, 8f));

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            Require(atlas != null && atlas.width == 128 && atlas.height == 96, "Industrial atlas must remain 128x96.");

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Require(ruleTile != null, "Rule Tile asset is missing.");
            Require(ruleTile.m_TilingRules.Count == 16, "Rule Tile must contain exactly 16 canonical rules.");
            for (int slot = 0; slot < 16; slot++)
            {
                RuleTile.TilingRule rule = ruleTile.m_TilingRules[slot];
                Require(rule.m_Sprites.Length == 1 && rule.m_Sprites[0].name == AtlasSpriteName(slot),
                    "Rule Tile sprite mismatch at canonical slot " + slot + ".");
                Require(rule.m_Neighbors.Count == 4 && rule.m_NeighborPositions.Count == 4,
                    "Rule Tile must test exactly four cardinal neighbors at slot " + slot + ".");
                int expectedMask = CanonicalConnectivityMasks[slot];
                for (int direction = 0; direction < 4; direction++)
                {
                    int expected = (expectedMask & (1 << direction)) != 0
                        ? RuleTile.TilingRuleOutput.Neighbor.This
                        : RuleTile.TilingRuleOutput.Neighbor.NotThis;
                    Require(rule.m_Neighbors[direction] == expected, "Connectivity mismatch at slot " + slot + ".");
                }
            }

            Dictionary<string, (int frameCount, float frameRate, bool loop)> clipSpecs =
                new Dictionary<string, (int frameCount, float frameRate, bool loop)>
                {
                    { "Player_Body_Idle", (2, 0.5f, true) },
                    { "Player_Body_Run", (6, 10f, true) },
                    { "Player_Body_Backpedal", (4, 7f, true) },
                    { "Player_Body_Jump", (3, JumpClipSampleRate, false) },
                    { "Player_Body_Fall", (1, 1f, false) },
                    { "Player_Body_Land", (2, 8f, true) },
                };
            foreach (KeyValuePair<string, (int frameCount, float frameRate, bool loop)> clipSpec in clipSpecs)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BodyAnimationRoot + "/" + clipSpec.Key + ".anim");
                Require(clip != null, "Animation clip missing: " + clipSpec.Key);
                Require(Mathf.Approximately(clip.frameRate, clipSpec.Value.frameRate),
                    clipSpec.Key + " frame rate mismatch.");
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Require(bindings.Length == 1, clipSpec.Key + " must contain one SpriteRenderer sprite curve.");
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Require(keyframes.Length == clipSpec.Value.frameCount,
                    clipSpec.Key + " must contain exactly one key per source frame.");
                string stateId = clipSpec.Key.Substring("Player_Body_".Length).ToLowerInvariant();
                for (int index = 0; index < keyframes.Length; index++)
                {
                    Require(keyframes[index].value != null &&
                            keyframes[index].value.name == "player_salvager_body_" + stateId + "_" + index,
                        clipSpec.Key + " sprite order mismatch at frame " + index + ".");
                }
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                Require(clipSettings.loopTime == clipSpec.Value.loop, clipSpec.Key + " loop setting mismatch.");

                if (clipSpec.Key == "Player_Body_Jump")
                {
                    Require(clip.wrapMode == WrapMode.ClampForever,
                        "Player_Body_Jump must hold its final frame instead of looping.");
                    for (int index = 0; index < keyframes.Length; index++)
                    {
                        Require(Mathf.Abs(keyframes[index].time - JumpTakeoffKeyframeTimes[index]) < 0.0001f,
                            "Player_Body_Jump key time mismatch at frame " + index + ".");
                    }
                }
            }

            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null, "ArtShowcase scene is missing.");
            Require(EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath && scene.enabled),
                "ArtShowcase must be enabled in build settings.");
            int movementLabIndex = Array.FindIndex(EditorBuildSettings.scenes,
                scene => scene.path == "Assets/Scenes/MovementLab.unity" && scene.enabled);
            if (movementLabIndex >= 0)
            {
                int showcaseIndex = Array.FindIndex(EditorBuildSettings.scenes,
                    scene => scene.path == ScenePath && scene.enabled);
                Require(movementLabIndex == 0 && showcaseIndex == 1,
                    "MovementLab and ArtShowcase must be the first two enabled build scenes.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string manifest = File.ReadAllText(Path.Combine(projectRoot, "Packages", "manifest.json"));
            Require(manifest.IndexOf("com.unity.multiplayer.center", StringComparison.OrdinalIgnoreCase) < 0,
                "Multiplayer Center is still present in Packages/manifest.json.");
        }

        private static void ValidateLongwatchIdleAimSheets()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string absoluteRoot = Path.Combine(projectRoot, LongwatchIdleAimRoot);
            Require(Directory.Exists(absoluteRoot), "Longwatch Idle aim source folder is missing.");
            string[] actualPngs = Directory.GetFiles(absoluteRoot, "*.png", SearchOption.TopDirectoryOnly);
            Require(actualPngs.Length == LongwatchDirections.Length,
                "Longwatch Idle aim folder must contain exactly the 19 expected direction PNGs.");

            HashSet<string> expectedFiles = new HashSet<string>(
                LongwatchDirections.Select(direction => direction.IdleFileName + ".png"),
                StringComparer.OrdinalIgnoreCase);
            foreach (string actualPath in actualPngs)
            {
                Require(expectedFiles.Contains(Path.GetFileName(actualPath)),
                    "Unexpected Longwatch Idle aim direction file: " + Path.GetFileName(actualPath));
            }

            foreach (LongwatchDirectionSpec direction in LongwatchDirections)
            {
                ValidateImporter(direction.IdleAssetPath);
                ValidateSpriteGrid(
                    direction.IdleAssetPath,
                    80,
                    96,
                    2,
                    false,
                    index => direction.IdleFileName + "_" + index,
                    new Vector2(24f, 8f));
                ValidateSourcePixels(direction.IdleAssetPath);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(direction.IdleAssetPath);
                Require(texture != null && texture.width == 160 && texture.height == 96,
                    direction.IdleAssetPath + " must remain exactly 160x96 (two 80x96 cells)." );
            }
        }

        private static void ValidateLongwatchRunAimSheets()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string absoluteRoot = Path.Combine(projectRoot, LongwatchRunAimRoot);
            Require(Directory.Exists(absoluteRoot), "Longwatch Run aim source folder is missing.");
            string[] actualPngs = Directory.GetFiles(absoluteRoot, "*.png", SearchOption.TopDirectoryOnly);
            Require(actualPngs.Length == LongwatchDirections.Length,
                "Longwatch Run aim folder must contain exactly the 19 expected direction PNGs.");

            HashSet<string> expectedFiles = new HashSet<string>(
                LongwatchDirections.Select(direction => direction.RunFileName + ".png"),
                StringComparer.OrdinalIgnoreCase);
            foreach (string actualPath in actualPngs)
            {
                Require(expectedFiles.Contains(Path.GetFileName(actualPath)),
                    "Unexpected Longwatch Run aim direction file: " + Path.GetFileName(actualPath));
            }

            int importedSpriteCount = 0;
            foreach (LongwatchDirectionSpec direction in LongwatchDirections)
            {
                ValidateImporter(direction.RunAssetPath);
                ValidateSpriteGrid(
                    direction.RunAssetPath,
                    80,
                    96,
                    6,
                    false,
                    index => direction.RunFileName + "_" + index,
                    new Vector2(24f, 8f));
                ValidateSourcePixels(direction.RunAssetPath);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(direction.RunAssetPath);
                Require(texture != null && texture.width == 480 && texture.height == 96,
                    direction.RunAssetPath + " must remain exactly 480x96 (six 80x96 cells)." );
                importedSpriteCount += LoadSprites(direction.RunAssetPath).Count();
            }

            Require(importedSpriteCount == 114,
                "Longwatch Run aim package must import exactly 114 sprites.");
        }

        private static void ValidateLongwatchBackpedalAimSheets()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string absoluteRoot = Path.Combine(projectRoot, LongwatchBackpedalAimRoot);
            Require(Directory.Exists(absoluteRoot), "Longwatch Backpedal aim source folder is missing.");
            string[] actualPngs = Directory.GetFiles(absoluteRoot, "*.png", SearchOption.TopDirectoryOnly);
            Require(actualPngs.Length == LongwatchDirections.Length,
                "Longwatch Backpedal aim folder must contain exactly the 19 expected direction PNGs.");

            HashSet<string> expectedFiles = new HashSet<string>(
                LongwatchDirections.Select(direction => direction.BackpedalFileName + ".png"),
                StringComparer.OrdinalIgnoreCase);
            foreach (string actualPath in actualPngs)
            {
                Require(expectedFiles.Contains(Path.GetFileName(actualPath)),
                    "Unexpected Longwatch Backpedal aim direction file: " + Path.GetFileName(actualPath));
            }

            int importedSpriteCount = 0;
            foreach (LongwatchDirectionSpec direction in LongwatchDirections)
            {
                ValidateImporter(direction.BackpedalAssetPath);
                ValidateSpriteGrid(
                    direction.BackpedalAssetPath,
                    80,
                    96,
                    4,
                    false,
                    index => direction.BackpedalFileName + "_" + index,
                    new Vector2(24f, 8f));
                ValidateSourcePixels(direction.BackpedalAssetPath);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(direction.BackpedalAssetPath);
                Require(texture != null && texture.width == 320 && texture.height == 96,
                    direction.BackpedalAssetPath + " must remain exactly 320x96 (four 80x96 cells)." );
                importedSpriteCount += LoadSprites(direction.BackpedalAssetPath).Count();
            }

            Require(importedSpriteCount == 76,
                "Longwatch Backpedal aim package must import exactly 76 sprites.");
        }

        private static void ValidateImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, "Missing texture importer: " + path);
            Require(importer.textureType == TextureImporterType.Sprite, path + " must import as Sprite.");
            Require(Mathf.Approximately(importer.spritePixelsPerUnit, 16f), path + " must use 16 PPU.");
            Require(importer.filterMode == FilterMode.Point, path + " must use Point filtering.");
            Require(!importer.mipmapEnabled, path + " must disable mipmaps.");
            Require(importer.textureCompression == TextureImporterCompression.Uncompressed, path + " must disable compression.");
            Require(!importer.crunchedCompression, path + " must disable crunch compression.");
            Require(importer.alphaIsTransparency, path + " must import alpha as transparency.");
            Require(importer.wrapMode == TextureWrapMode.Clamp, path + " must use Clamp wrapping.");
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Require(settings.spriteMeshType == SpriteMeshType.FullRect, path + " must use Full Rect meshes.");
            Require(!settings.spriteGenerateFallbackPhysicsShape,
                path + " must disable fallback physics-shape generation.");
            Require(importer.spriteImportMode == SpriteImportMode.Multiple, path + " must use fixed multiple-sprite slicing.");
        }

        private static void ValidateSourcePixels(string path)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve the Unity project root.");
            string absolutePath = Path.Combine(projectRoot, path);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                Require(source.LoadImage(File.ReadAllBytes(absolutePath), false), "Could not decode source PNG: " + path);
                Color32[] pixels = source.GetPixels32();
                for (int index = 0; index < pixels.Length; index++)
                {
                    Color32 pixel = pixels[index];
                    Require(pixel.a == 0 || pixel.a == 255,
                        path + " contains non-binary alpha at source pixel " + index + ".");
                    Require(pixel.a == 0 || RustlinePalette.IsCanonical(pixel),
                        path + " contains an opaque pixel outside Canonical 28 at source pixel " + index + ".");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void ValidateSpriteGrid(
            string path,
            int cellWidth,
            int cellHeight,
            int count,
            bool logicalRowsRunTopToBottom,
            Func<int, string> expectedName,
            Vector2 expectedPivotPixels)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture != null, "Missing imported texture: " + path);
            Require(texture.width % cellWidth == 0 && texture.height % cellHeight == 0,
                $"{path} dimensions {texture.width}x{texture.height} do not align to {cellWidth}x{cellHeight} cells.");
            int columns = texture.width / cellWidth;
            int rows = texture.height / cellHeight;
            Dictionary<string, Sprite> sprites = LoadSprites(path).ToDictionary(sprite => sprite.name);
            Require(sprites.Count == count, path + " sprite count mismatch.");

            for (int index = 0; index < count; index++)
            {
                string name = expectedName(index);
                Require(sprites.TryGetValue(name, out Sprite sprite), "Missing sprite: " + name);
                int column = index % columns;
                int logicalRow = index / columns;
                int unityRow = logicalRowsRunTopToBottom ? rows - 1 - logicalRow : logicalRow;
                Rect expectedRect = new Rect(column * cellWidth, unityRow * cellHeight, cellWidth, cellHeight);
                Require(sprite.rect == expectedRect, $"{name} rect is {sprite.rect}, expected {expectedRect}.");
                Require(Vector2.Distance(sprite.pivot, expectedPivotPixels) < 0.001f,
                    $"{name} pivot is {sprite.pivot}, expected {expectedPivotPixels}.");
            }
        }

        private static IEnumerable<Sprite> LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
        }

        private static int ParseTrailingIndex(string name)
        {
            int separator = name.LastIndexOf('_');
            int index = -1;
            bool hasNumericIndex = separator >= 0 && int.TryParse(name.Substring(separator + 1), out index);
            Require(hasNumericIndex,
                "Sprite name must end in a numeric frame index: " + name);
            return index;
        }

        private static string AtlasSpriteName(int index)
        {
            return "industrial_surface_" + index.ToString("00") + "_" + AtlasSlotSuffixes[index];
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
