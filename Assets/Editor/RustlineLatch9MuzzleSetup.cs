using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>
    /// Deterministically imports the two Latch-9 muzzle-flash sheets and converts the
    /// generated ArtSource JSON into compact runtime metadata. Safe to rerun.
    /// </summary>
    [InitializeOnLoad]
    public static class RustlineLatch9MuzzleSetup
    {
        private const string MetadataJsonPath =
            "ArtSource/Metadata/Weapons/latch_9/Generated/latch_9_muzzle_metadata.json";
        private const string MetadataAssetPath =
            "Assets/Config/Weapons/Generated/Latch9MuzzleMetadata.asset";
        private const string ConventionalFlashPath =
            "Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash.png";
        private const string BouncingFlashPath =
            "Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash_bouncing.png";

        private static readonly string[] DirectionSuffixes =
        {
            "p90", "p80", "p70", "p60", "p50", "p40", "p30", "p20", "p10", "0",
            "m10", "m20", "m30", "m40", "m50", "m60", "m70", "m80", "m90",
        };

        private static readonly int[] DirectionAngles =
        {
            90, 80, 70, 60, 50, 40, 30, 20, 10, 0,
            -10, -20, -30, -40, -50, -60, -70, -80, -90,
        };

        [Serializable]
        private sealed class MuzzleJson
        {
            public int schemaVersion;
            public int generatorVersion;
            public string weaponId;
            public int[] cellSizePixels;
            public int[] pivotPixels;
            public MuzzleReferenceJson reference;
            public MuzzleStateJson[] states;
        }

        [Serializable]
        private sealed class MuzzleReferenceJson
        {
            public string state;
            public int frame;
            public string path;
        }

        [Serializable]
        private sealed class MuzzleStateJson
        {
            public string name;
            public int frameCount;
            public MuzzleDirectionJson[] directions;
        }

        [Serializable]
        private sealed class MuzzleDirectionJson
        {
            public string suffix;
            public int angleDegrees;
            public bool supported;
            public MuzzleFrameJson[] frames;
        }

        [Serializable]
        private sealed class MuzzleFrameJson
        {
            public int frame;
            public float[] muzzleOffsetPixels;
        }

        static RustlineLatch9MuzzleSetup()
        {
            EditorApplication.delayCall += EnsureGeneratedAssets;
        }

        [MenuItem("Tools/Rustline/Weapons/Rebuild Latch-9 Muzzle Presentation")]
        public static void RebuildFromMenu()
        {
            BuildAndValidate();
            EditorUtility.DisplayDialog(
                "Latch-9 Muzzle Presentation",
                "Latch-9 muzzle-flash imports and runtime metadata were rebuilt and validated.",
                "OK");
        }

        public static void BuildAndValidate()
        {
            ConfigureFlashSheet(ConventionalFlashPath, "latch_9_muzzle_flash");
            ConfigureFlashSheet(BouncingFlashPath, "latch_9_muzzle_flash_bouncing");
            Latch9MuzzleMetadata2D metadata = CreateOrUpdateMetadata();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateFlashSheet(ConventionalFlashPath, "latch_9_muzzle_flash");
            ValidateFlashSheet(BouncingFlashPath, "latch_9_muzzle_flash_bouncing");
            ValidateRuntimeMetadata(metadata);
        }

        private static void EnsureGeneratedAssets()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                if (NeedsBuild())
                {
                    BuildAndValidate();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static bool NeedsBuild()
        {
            Latch9MuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<Latch9MuzzleMetadata2D>(MetadataAssetPath);
            if (metadata == null || metadata.SchemaVersion != 1 || metadata.GeneratorVersion != 2 ||
                metadata.WeaponId != "latch_9" ||
                metadata.CellSizePixels != new Vector2Int(80, 96) ||
                metadata.PivotPixels != new Vector2Int(24, 8) ||
                metadata.GetSupportedPointCount() != 361)
            {
                return true;
            }

            return !IsFlashSheetReady(ConventionalFlashPath, "latch_9_muzzle_flash") ||
                   !IsFlashSheetReady(BouncingFlashPath, "latch_9_muzzle_flash_bouncing");
        }

        private static bool IsFlashSheetReady(string path, string prefix)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple ||
                importer.spritePixelsPerUnit != 16f || importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                !importer.sRGBTexture || importer.npotScale != TextureImporterNPOTScale.None)
            {
                return false;
            }

            List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.rect.x)
                .ToList();
            if (sprites.Count != 20)
            {
                return false;
            }

            for (int index = 0; index < sprites.Count; index++)
            {
                string expectedName = prefix + "_v" + (index / 2).ToString("00") +
                    "_f" + (index % 2);
                if (sprites[index].name != expectedName ||
                    sprites[index].rect != new Rect(index * 9, 0, 9, 9) ||
                    !Mathf.Approximately(sprites[index].pivot.x, 0.5f) ||
                    !Mathf.Approximately(sprites[index].pivot.y, 4.5f))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureFlashSheet(string path, string prefix)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, "Latch-9 muzzle-flash importer is missing: " + path);

            RustlinePixelArtPostprocessor.ApplyBaseline(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture != null && texture.width == 180 && texture.height == 9,
                "Latch-9 muzzle-flash sheet must be exactly 180x9: " + path);

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            Require(provider != null, "Sprite data provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .GroupBy(rect => rect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);

            SpriteRect[] rects = new SpriteRect[20];
            for (int index = 0; index < rects.Length; index++)
            {
                string name = prefix + "_v" + (index / 2).ToString("00") +
                    "_f" + (index % 2);
                rects[index] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(index * 9, 0, 9, 9),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f / 9f, 0.5f),
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(name, out GUID existingId)
                        ? existingId
                        : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider nameProvider =
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            Require(nameProvider != null, "Sprite name/file-ID provider unavailable: " + path);
            nameProvider.SetNameFileIdPairs(
                rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static Latch9MuzzleMetadata2D CreateOrUpdateMetadata()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Require(!string.IsNullOrEmpty(projectRoot), "Could not resolve Unity project root.");
            string absoluteJsonPath = Path.Combine(projectRoot, MetadataJsonPath);
            Require(File.Exists(absoluteJsonPath),
                "Generated Latch-9 muzzle metadata JSON is missing: " + MetadataJsonPath);

            MuzzleJson source = JsonUtility.FromJson<MuzzleJson>(File.ReadAllText(absoluteJsonPath));
            ValidateSourceJson(source);

            Latch9MuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<Latch9MuzzleMetadata2D>(MetadataAssetPath);
            if (metadata == null)
            {
                metadata = ScriptableObject.CreateInstance<Latch9MuzzleMetadata2D>();
                metadata.name = "Latch-9 Muzzle Metadata";
                AssetDatabase.CreateAsset(metadata, MetadataAssetPath);
            }

            SerializedObject serialized = new SerializedObject(metadata);
            serialized.FindProperty("schemaVersion").intValue = source.schemaVersion;
            serialized.FindProperty("generatorVersion").intValue = source.generatorVersion;
            serialized.FindProperty("weaponId").stringValue = source.weaponId;
            serialized.FindProperty("cellSizePixels").vector2IntValue =
                new Vector2Int(source.cellSizePixels[0], source.cellSizePixels[1]);
            serialized.FindProperty("pivotPixels").vector2IntValue =
                new Vector2Int(source.pivotPixels[0], source.pivotPixels[1]);
            ConfigureDirectionArray(serialized.FindProperty("idleDirections"), source.states[0]);
            ConfigureDirectionArray(serialized.FindProperty("runDirections"), source.states[1]);
            ConfigureDirectionArray(serialized.FindProperty("backpedalDirections"), source.states[2]);
            ConfigureDirectionArray(serialized.FindProperty("crouchDirections"), source.states[3]);
            ConfigureDirectionArray(serialized.FindProperty("fallDirections"), source.states[4]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(metadata);
            return metadata;
        }

        private static void ValidateSourceJson(MuzzleJson source)
        {
            Require(source != null, "Generated Latch-9 muzzle metadata JSON could not be parsed.");
            Require(source.schemaVersion == 1 && source.generatorVersion == 2 &&
                source.weaponId == "latch_9",
                "Generated Latch-9 muzzle metadata identity mismatch.");
            Require(source.cellSizePixels != null && source.cellSizePixels.Length == 2 &&
                source.cellSizePixels[0] == 80 && source.cellSizePixels[1] == 96 &&
                source.pivotPixels != null && source.pivotPixels.Length == 2 &&
                source.pivotPixels[0] == 24 && source.pivotPixels[1] == 8,
                "Generated Latch-9 muzzle metadata geometry mismatch.");
            Require(source.reference != null && source.reference.state == "Idle" &&
                source.reference.frame == 0 &&
                source.reference.path ==
                    "ArtSource/Metadata/Weapons/latch_9/Muzzle/latch_9_muzzle_reference.png",
                "Generated Latch-9 muzzle metadata reference mismatch.");

            string[] stateNames = { "Idle", "Run", "Backpedal", "Crouch", "Fall" };
            int[] frameCounts = { 2, 6, 4, 6, 1 };
            Require(source.states != null && source.states.Length == stateNames.Length,
                "Generated Latch-9 muzzle metadata must contain five states.");

            int supportedPointCount = 0;
            for (int stateIndex = 0; stateIndex < stateNames.Length; stateIndex++)
            {
                MuzzleStateJson state = source.states[stateIndex];
                Require(state != null && state.name == stateNames[stateIndex] &&
                    state.frameCount == frameCounts[stateIndex] &&
                    state.directions != null &&
                    state.directions.Length == Latch9MuzzleMetadata2D.DirectionCount,
                    "Generated Latch-9 muzzle state mismatch at index " + stateIndex + ".");

                for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
                {
                    MuzzleDirectionJson direction = state.directions[directionIndex];
                    Require(direction != null &&
                        direction.suffix == DirectionSuffixes[directionIndex] &&
                        direction.angleDegrees == DirectionAngles[directionIndex] &&
                        direction.supported &&
                        direction.frames != null &&
                        direction.frames.Length == state.frameCount,
                        $"Generated Latch-9 muzzle direction mismatch at {state.name}/{directionIndex}.");

                    for (int frameIndex = 0; frameIndex < state.frameCount; frameIndex++)
                    {
                        MuzzleFrameJson frame = direction.frames[frameIndex];
                        Require(frame != null && frame.frame == frameIndex &&
                            frame.muzzleOffsetPixels != null &&
                            frame.muzzleOffsetPixels.Length == 2 &&
                            float.IsFinite(frame.muzzleOffsetPixels[0]) &&
                            float.IsFinite(frame.muzzleOffsetPixels[1]) &&
                            IsHalfInteger(frame.muzzleOffsetPixels[0]) &&
                            IsHalfInteger(frame.muzzleOffsetPixels[1]),
                            $"Generated Latch-9 muzzle coordinate mismatch at " +
                            $"{state.name}/{direction.suffix}/frame {frameIndex}.");
                        supportedPointCount++;
                    }
                }
            }

            Require(supportedPointCount == 361,
                "Generated Latch-9 muzzle metadata must contain exactly 361 supported points.");
        }

        private static void ConfigureDirectionArray(
            SerializedProperty target,
            MuzzleStateJson source)
        {
            Require(target != null && target.isArray,
                "Latch-9 runtime muzzle metadata direction array is unavailable.");
            target.arraySize = source.directions.Length;
            for (int directionIndex = 0; directionIndex < source.directions.Length; directionIndex++)
            {
                MuzzleDirectionJson sourceDirection = source.directions[directionIndex];
                SerializedProperty targetDirection = target.GetArrayElementAtIndex(directionIndex);
                targetDirection.FindPropertyRelative("suffix").stringValue = sourceDirection.suffix;
                targetDirection.FindPropertyRelative("angleDegrees").intValue =
                    sourceDirection.angleDegrees;
                targetDirection.FindPropertyRelative("supported").boolValue = true;
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

        private static void ValidateRuntimeMetadata(Latch9MuzzleMetadata2D metadata)
        {
            Require(metadata != null && metadata.SchemaVersion == 1 &&
                metadata.GeneratorVersion == 2 && metadata.WeaponId == "latch_9" &&
                metadata.CellSizePixels == new Vector2Int(80, 96) &&
                metadata.PivotPixels == new Vector2Int(24, 8) &&
                metadata.GetSupportedPointCount() == 361,
                "Generated Latch-9 runtime muzzle metadata asset is invalid.");

            Latch9MuzzleState2D[] states =
            {
                Latch9MuzzleState2D.Idle,
                Latch9MuzzleState2D.Run,
                Latch9MuzzleState2D.Backpedal,
                Latch9MuzzleState2D.Crouch,
                Latch9MuzzleState2D.Fall,
            };
            int[] frameCounts = { 2, 6, 4, 6, 1 };
            for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
                {
                    Latch9MuzzleDirection2D direction =
                        metadata.GetDirection(states[stateIndex], directionIndex);
                    Require(direction.Suffix == DirectionSuffixes[directionIndex] &&
                        direction.AngleDegrees == DirectionAngles[directionIndex] &&
                        direction.Supported &&
                        direction.FrameCount == frameCounts[stateIndex],
                        $"Runtime Latch-9 muzzle metadata mismatch at " +
                        $"{states[stateIndex]}/{directionIndex}.");
                }
            }
        }

        private static void ValidateFlashSheet(string path, string prefix)
        {
            Require(IsFlashSheetReady(path, prefix),
                "Latch-9 muzzle-flash import contract is invalid: " + path);
        }

        private static bool IsHalfInteger(float value)
        {
            return Mathf.Approximately(value * 2f, Mathf.Round(value * 2f)) &&
                   !Mathf.Approximately(value, Mathf.Round(value));
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
