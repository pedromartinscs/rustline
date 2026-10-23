using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>Subset-only, deterministic pixel fragmentation of the accepted final wreck.</summary>
    public static class RustlineBombardierDissolutionSetup
    {
        public const string SheetPath = "Assets/Art/Characters/Enemies/Bombardier/bombardier_dissolution.png";

        public static void RebuildSprites()
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(144, 64, TextureFormat.RGBA32, false);
            try
            {
                original.LoadImage(File.ReadAllBytes(RustlineBombardierArtSetup.SheetPath));
                Color32[] input = original.GetPixels32();
                var output = new Color32[144 * 64];
                int[] thresholds = { 72, 39, 13 };
                for (int frame = 0; frame < 3; frame++)
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 48; x++)
                        {
                            // Stable two-pixel fragments. Every later mask is a strict subset;
                            // surviving pixels never move or change RGB/alpha.
                            uint hash = unchecked((uint)((x / 2 + 17) * 73856093 ^ (y / 2 + 31) * 19349663));
                            hash ^= hash >> 13;
                            if (hash % 100 < thresholds[frame])
                                output[y * 144 + frame * 48 + x] = input[y * 192 + 144 + x];
                        }
                sheet.SetPixels32(output); sheet.Apply();
                File.WriteAllBytes(SheetPath, sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(sheet); }
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            RustlinePixelArtPostprocessor.ApplyBaseline(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new SpriteRect[3];
            for (int i = 0; i < 3; i++)
            {
                string name = "bombardier_dissolve_" + i;
                rects[i] = new SpriteRect { name = name, rect = new Rect(i * 48, 0, 48, 64),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 0f),
                    spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate() };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
        }

        public static Sprite[] LoadFrames()
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 3) throw new InvalidOperationException("Rebuild Bombardier dissolution art before authoring.");
            return frames;
        }
    }
}

