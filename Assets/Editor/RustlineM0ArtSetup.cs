using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string AtlasPath = "Assets/Art/Environment/Tiles/industrial_surface.png";
        private const string AnimationRoot = PlayerRoot + "/Animations";
        private const string TileAssetRoot = "Assets/Art/Environment/Tiles/Generated";
        private const string RuleTilePath = TileAssetRoot + "/IndustrialSurfaceRuleTile.asset";
        private const string ScenePath = "Assets/Scenes/ArtShowcase.unity";

        private static readonly SheetSpec[] PlayerSheets =
        {
            new SheetSpec("player_salvager_base_right", 1),
            new SheetSpec("player_salvager_idle", 2),
            new SheetSpec("player_salvager_run", 6),
            new SheetSpec("player_salvager_jump", 3),
            new SheetSpec("player_salvager_fall", 1),
            new SheetSpec("player_salvager_land", 2),
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
            internal SheetSpec(string fileName, int frameCount)
            {
                FileName = fileName;
                FrameCount = frameCount;
            }

            internal string FileName { get; }
            internal int FrameCount { get; }
            internal string AssetPath => PlayerRoot + "/" + FileName + ".png";
        }

        private sealed class PreviewAsset
        {
            internal PreviewAsset(string label, AnimationClip clip, RuntimeAnimatorController controller, Sprite firstSprite)
            {
                Label = label;
                Clip = clip;
                Controller = controller;
                FirstSprite = firstSprite;
            }

            internal string Label { get; }
            internal AnimationClip Clip { get; }
            internal RuntimeAnimatorController Controller { get; }
            internal Sprite FirstSprite { get; }
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
            EnsureFolder(TileAssetRoot);

            ConfigurePlayerSheets();
            ConfigureEnvironmentAtlas();

            Dictionary<string, PreviewAsset> previews = CreateAnimationPreviews();
            RuleTile ruleTile = CreateRuleTile();
            CreateShowcaseScene(previews, ruleTile);
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
            AddPreview(previews, "Idle", "player_salvager_idle", "Player_Idle", 3f, true);
            AddPreview(previews, "Run", "player_salvager_run", "Player_Run", 10f, true);
            AddPreview(previews, "Jump", "player_salvager_jump", "Player_Jump", 6f, true);
            AddPreview(previews, "Fall", "player_salvager_fall", "Player_Fall", 1f, false);
            AddPreview(previews, "Land", "player_salvager_land", "Player_Land", 8f, true);
            return previews;
        }

        private static void AddPreview(
            IDictionary<string, PreviewAsset> previews,
            string label,
            string sheetName,
            string clipName,
            float frameRate,
            bool loop)
        {
            string sheetPath = PlayerRoot + "/" + sheetName + ".png";
            List<Sprite> frames = LoadSprites(sheetPath)
                .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                .ToList();
            Require(frames.Count > 0, "No frames found for " + sheetPath);

            string clipPath = AnimationRoot + "/" + clipName + ".anim";
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
            for (int index = 0; index < frames.Count; index++)
            {
                keyframes.Add(new ObjectReferenceKeyframe
                {
                    time = index / frameRate,
                    value = frames[index],
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
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                stateMachine.RemoveState(childState.state);
            }

            AnimatorState state = stateMachine.AddState(clipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);

            previews.Add(label, new PreviewAsset(label, clip, controller, frames[0]));
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
            string[] order = { "Idle", "Run", "Jump", "Fall", "Land" };
            float[] xPositions = { -16f, -8f, 0f, 8f, 16f };

            for (int index = 0; index < order.Length; index++)
            {
                PreviewAsset preview = previews[order[index]];
                GameObject specimen = new GameObject("Player_" + preview.Label + "_Specimen");
                specimen.transform.SetParent(specimensRoot.transform, false);
                specimen.transform.position = new Vector3(xPositions[index], 6f, 0f);

                SpriteRenderer renderer = specimen.AddComponent<SpriteRenderer>();
                renderer.sprite = preview.FirstSprite;
                renderer.sortingOrder = 5;

                Animator animator = specimen.AddComponent<Animator>();
                animator.runtimeAnimatorController = preview.Controller;

                CreateLabel(
                    labelsRoot,
                    preview.Label.ToUpperInvariant(),
                    new Vector3(xPositions[index], 10.65f, -0.2f),
                    0.16f,
                    new Color32(253, 208, 69, 255));
            }
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
                    { "Player_Idle", (2, 3f, true) },
                    { "Player_Run", (6, 10f, true) },
                    { "Player_Jump", (3, 6f, true) },
                    { "Player_Fall", (1, 1f, false) },
                    { "Player_Land", (2, 8f, true) },
                };
            foreach (KeyValuePair<string, (int frameCount, float frameRate, bool loop)> clipSpec in clipSpecs)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/" + clipSpec.Key + ".anim");
                Require(clip != null, "Animation clip missing: " + clipSpec.Key);
                Require(Mathf.Approximately(clip.frameRate, clipSpec.Value.frameRate),
                    clipSpec.Key + " frame rate mismatch.");
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Require(bindings.Length == 1, clipSpec.Key + " must contain one SpriteRenderer sprite curve.");
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Require(keyframes.Length == clipSpec.Value.frameCount,
                    clipSpec.Key + " must contain exactly one key per source frame.");
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                Require(clipSettings.loopTime == clipSpec.Value.loop, clipSpec.Key + " loop setting mismatch.");
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
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Require(settings.spriteMeshType == SpriteMeshType.FullRect, path + " must use Full Rect meshes.");
            Require(importer.spriteImportMode == SpriteImportMode.Multiple, path + " must use fixed multiple-sprite slicing.");
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
