using System;
using System.IO;
using System.Linq;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>Original explosion conversion and narrow saved-enemy VFX authoring.</summary>
    public static class RustlineCombatVfxSetup
    {
        public const string SourcePath = "Assets/Art/Source/Effects/Bombardier/explosion_generated_source.png";
        public const string SheetPath = "Assets/Art/Effects/Explosions/Bombardier/bombardier_explosion.png";

        public static void RebuildAndApply()
        {
            RebuildExplosionSprites();
            RustlineBombardierDissolutionSetup.RebuildSprites();
            foreach (string path in new[] { "Assets/Scenes/MovementLab.unity", RustlineFirstPlayableSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var enemy in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Bombardier2D>(true)))
                {
                    string before = Snapshot(enemy);
                    RustlineBombardierArtSetup.Configure(enemy);
                    changed |= before != Snapshot(enemy);
                }
                RustlineBombardierArtSetup.ValidateScene(scene);
                // Avoid serializing unrelated RuleTile caches when references already match.
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }
            Debug.Log("RUSTLINE_COMBAT_VFX_OK: six explosion keys and three Bombardier dissolution keys; both saved scenes configured.");
        }

        private static string Snapshot(Bombardier2D enemy)
        {
            var presenter = enemy.GetComponentInChildren<BombardierPresenter2D>(true);
            return EditorJsonUtility.ToJson(enemy) + (presenter == null ? "" :
                EditorJsonUtility.ToJson(presenter) + EditorJsonUtility.ToJson(presenter.BodyRenderer) +
                EditorJsonUtility.ToJson(presenter.transform));
        }

        public static void ValidateRegeneration()
        {
            string[] paths = { SheetPath, SheetPath + ".meta", SourcePath, SourcePath + ".meta",
                RustlineBombardierDissolutionSetup.SheetPath, RustlineBombardierDissolutionSetup.SheetPath + ".meta",
                RustlineBombardierArtSetup.SheetPath, RustlineBombardierArtSetup.SheetPath + ".meta",
                RustlineSentryArtSetup.SheetPath, RustlineSentryArtSetup.SheetPath + ".meta",
                RustlineFirstPlayableSetup.ScenePath, "Assets/Scenes/MovementLab.unity" };
            var originals = paths.Select(File.ReadAllBytes).ToArray();
            for (int pass = 0; pass < 2; pass++)
            {
                RebuildAndApply();
                for (int i = 0; i < paths.Length; i++)
                    if (!originals[i].SequenceEqual(File.ReadAllBytes(paths[i])))
                        throw new InvalidOperationException("VFX regeneration changed bytes: " + paths[i]);
            }
            // Also verify the existing M4/M5 source pipeline with the new scene references.
            RustlineBombardierArtSetup.ValidateRegeneration();
            for (int i = 0; i < paths.Length; i++)
                if (!originals[i].SequenceEqual(File.ReadAllBytes(paths[i])))
                    throw new InvalidOperationException("Upstream art regeneration changed VFX bytes: " + paths[i]);
            Debug.Log("RUSTLINE_COMBAT_VFX_DETERMINISTIC_OK: two passes, twelve production/source/meta/scene files byte-identical; M4/M5 pipeline preserved.");
        }

        public static void RebuildExplosionSprites()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(192, 128, TextureFormat.RGBA32, false);
            try
            {
                source.LoadImage(File.ReadAllBytes(SourcePath));
                if (source.width != 1536 || source.height != 1024)
                    throw new InvalidOperationException("Expected reviewed 1536x1024 explosion source.");
                Color32[] input = source.GetPixels32();
                var output = new Color32[192 * 128];
                for (int frame = 0; frame < 6; frame++)
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 64; x++)
                        {
                            float dx = x + 0.5f - 32f, dy = y + 0.5f - 32f;
                            if (dx * dx + dy * dy > 28f * 28f) continue;
                            // Fixed cell center and one 9:1 point scale; no per-pose resizing.
                            int sx = frame % 3 * 512 + Mathf.FloorToInt(256f + dx * 9f);
                            int sy = (1 - frame / 3) * 512 + Mathf.FloorToInt(256f + dy * 9f);
                            if (sx < frame % 3 * 512 || sx >= (frame % 3 + 1) * 512 ||
                                sy < (1 - frame / 3) * 512 || sy >= (2 - frame / 3) * 512) continue;
                            Color32 pixel = input[sy * 1536 + sx];
                            if (pixel.a < 200) continue; // Remove source halo/partial-alpha pixels.
                            output[((1 - frame / 3) * 64 + y) * 192 + frame % 3 * 64 + x] = NearestCanonical(pixel);
                        }
                sheet.SetPixels32(output); sheet.Apply();
                File.WriteAllBytes(SheetPath, sheet.EncodeToPNG());
                SavePreview(output);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(sheet); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSource();
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            RustlinePixelArtPostprocessor.ApplyBaseline(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new SpriteRect[6];
            for (int i = 0; i < 6; i++)
            {
                string name = "bombardier_explosion_" + i;
                rects[i] = new SpriteRect { name = name, rect = new Rect(i % 3 * 64, (1 - i / 3) * 64, 64, 64),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 0.5f),
                    spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate() };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
        }

        private static Color32 NearestCanonical(Color32 pixel)
        {
            int distance = int.MaxValue; Color32 best = default;
            foreach (Color32 color in RustlinePalette.CanonicalColors)
            {
                int dr = pixel.r - color.r, dg = pixel.g - color.g, db = pixel.b - color.b;
                int candidate = dr * dr + dg * dg + db * db;
                if (candidate >= distance) continue;
                distance = candidate; best = color;
            }
            return best;
        }

        private static void ConfigureSource()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
            if (importer.textureType == TextureImporterType.Default && importer.spriteImportMode == SpriteImportMode.None) return;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider != null)
            {
                provider.InitSpriteEditorDataProvider(); provider.SetSpriteRects(Array.Empty<SpriteRect>());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(Array.Empty<SpriteNameFileIdPair>());
                provider.Apply();
            }
            importer.textureType = TextureImporterType.Default; importer.spriteImportMode = SpriteImportMode.None;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        public static Sprite[] LoadExplosionFrames()
        {
            var frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 6) throw new InvalidOperationException("Rebuild combat VFX before authoring Bombardiers.");
            return frames;
        }

        private static void SavePreview(Color32[] pixels)
        {
            var preview = new Texture2D(768, 512, TextureFormat.RGBA32, false);
            try
            {
                var enlarged = new Color32[768 * 512];
                for (int y = 0; y < 512; y++)
                    for (int x = 0; x < 768; x++)
                    {
                        var pixel = pixels[y / 4 * 192 + x / 4];
                        enlarged[y * 768 + x] = pixel.a == 0 ? RustlinePalette.GetColor(1) : pixel;
                    }
                preview.SetPixels32(enlarged); preview.Apply();
                Directory.CreateDirectory("Logs/CombatVfx");
                File.WriteAllBytes("Logs/CombatVfx/explosion-contact-4x.png", preview.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
        }
    }
}
