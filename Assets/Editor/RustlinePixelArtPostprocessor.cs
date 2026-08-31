using System;
using UnityEditor;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>
    /// Applies the small set of import defaults shared by Rustline production pixel art.
    /// Sheet-specific fixed-grid slicing remains in <see cref="RustlineM0ArtSetup"/>.
    /// </summary>
    public sealed class RustlinePixelArtPostprocessor : AssetPostprocessor
    {
        internal const float PixelsPerUnit = 16f;

        private void OnPreprocessTexture()
        {
            if (!IsProductionPixelArt(assetPath))
            {
                return;
            }

            ApplyBaseline((TextureImporter)assetImporter);
        }

        internal static bool IsProductionPixelArt(string path)
        {
            string normalized = path.Replace('\\', '/');
            return normalized.StartsWith("Assets/Art/", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                && normalized.IndexOf("/Source/", StringComparison.OrdinalIgnoreCase) < 0;
        }

        internal static void ApplyBaseline(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 16384;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
