using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>Reviewed source conversion, stable slicing and enemy-only scene authoring.</summary>
    public static class RustlineBombardierArtSetup
    {
        public const string SourcePath = "Assets/Art/Source/Enemies/Bombardier/bombardier_generated_source.png";
        public const string SheetPath = "Assets/Art/Characters/Enemies/Bombardier/bombardier_demolition_sheet.png";
        public const int CellWidth = 48;
        public const int CellHeight = 64;
        private const int AlphaThreshold = 200;
        private const float SourcePixelsPerPixel = 7f;
        private static readonly int[] RowTop = { 50, 400, 740, 1120 };
        private static readonly int[] RowBottom = { 362, 724, 1086, 1420 };
        private static readonly string[] Names =
        {
            "idle_0", "idle_1", "walk_0", "walk_1", "walk_2", "walk_3", "windup_0",
            "windup_1", "windup_2", "launch", "recovery", "hit", "death_0", "death_1", "death_2", "death_3"
        };

        [MenuItem("Tools/Rustline/Bombardier/Rebuild Art and Apply to Salvage Intake")]
        public static void RebuildAndApply()
        {
            RebuildSprites();
            Scene scene = EditorSceneManager.OpenScene(RustlineFirstPlayableSetup.ScenePath, OpenSceneMode.Single);
            foreach (Bombardier2D enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Bombardier2D>(true)))
                Configure(enemy);
            ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            // MovementLab shares stable sprite references; its own M1A builder calls Configure.
            Debug.Log("RUSTLINE_BOMBARDIER_ART_OK: sixteen canonical poses, six saved sprite Bombardiers; gameplay/terrain preserved.");
        }

        /// <summary>Explicit authoring validation, not a runtime or automatic import operation.</summary>
        public static void ValidateRegeneration()
        {
            string[] paths = { SheetPath, SheetPath + ".meta", SourcePath, SourcePath + ".meta",
                RustlineSentryArtSetup.SheetPath, RustlineSentryArtSetup.SheetPath + ".meta",
                RustlineSentryArtSetup.SourcePath, RustlineSentryArtSetup.SourcePath + ".meta",
                RustlineSentryDissolutionSetup.SheetPath, RustlineSentryDissolutionSetup.SheetPath + ".meta",
                RustlineFirstPlayableSetup.ScenePath, "Assets/Scenes/MovementLab.unity" };
            byte[][] originals = paths.Select(File.ReadAllBytes).ToArray();
            for (int pass = 0; pass < 2; pass++)
            {
                RustlineSentryArtSetup.RebuildAndApply();
                RebuildAndApply();
                for (int i = 0; i < paths.Length; i++)
                    if (!originals[i].SequenceEqual(File.ReadAllBytes(paths[i])))
                        throw new InvalidOperationException("Enemy art regeneration changed bytes: " + paths[i]);
            }
            Debug.Log("RUSTLINE_ENEMY_ART_DETERMINISTIC_OK: two passes; twelve PNG/meta/source/scene files byte-identical.");
        }

        public static void RebuildSprites()
        {
            ConfigureReferenceSource();
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(CellWidth * 4, CellHeight * 4, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(SourcePath)) || source.width != 1086 || source.height != 1448)
                    throw new InvalidOperationException("Expected the reviewed original Bombardier source at 1086x1448.");
                Color32[] input = source.GetPixels32();
                var output = new Color32[sheet.width * sheet.height];
                for (int frame = 0; frame < Names.Length; frame++)
                {
                    int row = frame / 4;
                    int column = frame % 4;
                    int left = Mathf.RoundToInt(column * source.width / 4f);
                    int right = Mathf.RoundToInt((column + 1) * source.width / 4f);
                    // Common horizontal reference leaves room for the open throwing claw.
                    float center = (left + right) * 0.5f + 14f;
                    if (frame == 9) right = 590;
                    if (frame == 10) left = 600;
                    if (frame == 13) right = 523;
                    if (frame == 14) left = 527;
                    // Reviewed windows exclude neighboring parts without per-pose stretching.
                    int bottom = -1;
                    for (int topY = RowTop[row]; topY < RowBottom[row]; topY++)
                        for (int x = left; x < right; x++)
                            if (input[(source.height - 1 - topY) * source.width + x].a >= AlphaThreshold) bottom = topY;
                    if (bottom < 0) throw new InvalidOperationException("Empty source Bombardier pose: " + frame);
                    for (int y = 0; y < CellHeight; y++)
                        for (int x = 0; x < CellWidth; x++)
                        {
                            int sx = Mathf.FloorToInt(center + (x + 0.5f - CellWidth * 0.5f) * SourcePixelsPerPixel);
                            int topY = Mathf.FloorToInt(bottom + 1f - (y + 0.5f) * SourcePixelsPerPixel);
                            if (sx < left || sx >= right || topY < RowTop[row] || topY >= RowBottom[row]) continue;
                            Color32 pixel = input[(source.height - 1 - topY) * source.width + sx];
                            if (pixel.a < AlphaThreshold) continue;
                            int destination = ((3 - row) * CellHeight + y) * sheet.width + column * CellWidth + x;
                            output[destination] = NearestCanonical(pixel);
                        }
                }
                sheet.SetPixels32(output);
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(SheetPath));
                File.WriteAllBytes(SheetPath, sheet.EncodeToPNG());
                SaveReviewPreview(output, sheet.width, sheet.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport);
            Slice();
            RustlineBombardierDissolutionSetup.RebuildSprites();
        }

        private static void ConfigureReferenceSource()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
            // A source reference must not expose automatically sliced, noncanonical sprites
            // as selectable gameplay assets. Its original file bytes remain untouched.
            if (importer.textureType == TextureImporterType.Default && importer.spriteImportMode == SpriteImportMode.None) return;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider != null)
            {
                provider.InitSpriteEditorDataProvider();
                provider.SetSpriteRects(Array.Empty<SpriteRect>());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(Array.Empty<SpriteNameFileIdPair>());
                provider.Apply();
            }
            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Color32 NearestCanonical(Color32 pixel)
        {
            int bestDistance = int.MaxValue;
            Color32 best = default;
            foreach (Color32 color in RustlinePalette.CanonicalColors)
            {
                int dr = pixel.r - color.r, dg = pixel.g - color.g, db = pixel.b - color.b;
                int distance = dr * dr + dg * dg + db * db;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = color;
            }
            return best;
        }

        private static void SaveReviewPreview(Color32[] pixels, int width, int height)
        {
            const int scale = 4;
            var preview = new Texture2D(width * scale, height * scale, TextureFormat.RGBA32, false);
            try
            {
                var enlarged = new Color32[width * height * scale * scale];
                for (int y = 0; y < height * scale; y++)
                    for (int x = 0; x < width * scale; x++)
                    {
                        Color32 pixel = pixels[(y / scale) * width + x / scale];
                        enlarged[y * width * scale + x] = pixel.a == 0 ? RustlinePalette.GetColor(1) : pixel;
                    }
                preview.SetPixels32(enlarged); preview.Apply();
                Directory.CreateDirectory("Logs/BombardierArt");
                File.WriteAllBytes("Logs/BombardierArt/contact-sheet-4x.png", preview.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
        }

        private static void Slice()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            RustlinePixelArtPostprocessor.ApplyBaseline(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new SpriteRect[Names.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                string name = "bombardier_" + Names[i];
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i % 4 * CellWidth, (3 - i / 4) * CellHeight, CellWidth, CellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate()
                };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        public static Sprite[] LoadFrames()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            return Names.Select(name => sprites.TryGetValue("bombardier_" + name, out Sprite sprite) ? sprite :
                throw new InvalidOperationException("Bombardier art missing; run Rebuild Art first: " + name)).ToArray();
        }

        public static void Configure(Bombardier2D enemy, Material projectileMaterial = null)
        {
            if (projectileMaterial == null) projectileMaterial = enemy.ProjectileMaterial;
            // Read the old material once during migration; no renderer remains as a provider.
            foreach (string name in new[] { "Temporary Enemy Silhouette", "Programmer Art Presentation" })
            {
                Transform legacy = enemy.transform.Find(name);
                if (legacy == null) continue;
                var line = legacy.GetComponent<LineRenderer>();
                if (projectileMaterial == null && line != null) projectileMaterial = line.sharedMaterial;
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            }
            if (projectileMaterial == null) throw new InvalidOperationException("Bombardier projectile material missing: " + enemy.name);
            var actor = new SerializedObject(enemy);
            actor.FindProperty("projectileMaterial").objectReferenceValue = projectileMaterial;
            Sprite[] explosion = RustlineCombatVfxSetup.LoadExplosionFrames();
            SerializedProperty explosionArray = actor.FindProperty("explosionFrames");
            explosionArray.arraySize = explosion.Length;
            for (int i = 0; i < explosion.Length; i++) explosionArray.GetArrayElementAtIndex(i).objectReferenceValue = explosion[i];
            actor.ApplyModifiedPropertiesWithoutUndo();
            Transform visual = enemy.transform.Find("Bombardier Visual");
            if (visual == null)
            {
                visual = new GameObject("Bombardier Visual").transform;
                visual.SetParent(enemy.transform, false);
            }
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            Sprite[] frames = LoadFrames();
            renderer.enabled = true;
            renderer.sprite = frames[0];
            renderer.sortingOrder = 20;
            renderer.sortingLayerID = 0;
            renderer.color = Color.white;
            renderer.flipX = enemy.InitialDirection < 0;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            var presenter = visual.GetComponent<BombardierPresenter2D>();
            if (presenter == null) presenter = visual.gameObject.AddComponent<BombardierPresenter2D>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("enemy").objectReferenceValue = enemy;
            serialized.FindProperty("health").objectReferenceValue = enemy.GetComponent<CombatHealth2D>();
            serialized.FindProperty("markerMaterial").objectReferenceValue = projectileMaterial;
            serialized.FindProperty("bodyRenderer").objectReferenceValue = renderer;
            SerializedProperty array = serialized.FindProperty("frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            Sprite[] dissolution = RustlineBombardierDissolutionSetup.LoadFrames();
            array = serialized.FindProperty("dissolutionFrames");
            array.arraySize = dissolution.Length;
            for (int i = 0; i < dissolution.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = dissolution[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ValidateScene(Scene scene)
        {
            foreach (Bombardier2D enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Bombardier2D>(true)))
            {
                var presenter = enemy.GetComponentInChildren<BombardierPresenter2D>(true);
                if (presenter == null || !presenter.HasCompleteFrames || !enemy.HasExplosionFrames ||
                    presenter.BodyRenderer == null || presenter.BodyRenderer.sprite == null || enemy.ProjectileMaterial == null ||
                    presenter.transform.localScale != Vector3.one || enemy.GetComponentInChildren<LineRenderer>(true) != null)
                    throw new InvalidOperationException("Bombardier sprite presentation incomplete: " + enemy.name);
            }
        }
    }
}

