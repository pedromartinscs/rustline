using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Small deterministic pixel HUD font with wider special glyph support.</summary>
    public static class WeaponHudBitmapFont
    {
        public const int GlyphWidth = 5;
        public const int InfinityGlyphWidth = 9;
        public const int GlyphHeight = 7;
        public const int CellWidth = 6;
        public const int CellHeight = 8;
        private const int AtlasCellWidth = 10;
        private const int Columns = 8;
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -/()×∞";
        // HUD text is rebuilt on the main thread. Mesh.SetVertices/SetUVs/SetTriangles
        // copy these buffers, so the next line can reuse them without allocating lists.
        private static readonly List<Vector3> Vertices = new List<Vector3>(128);
        private static readonly List<Vector2> Uvs = new List<Vector2>(128);
        private static readonly List<int> Triangles = new List<int>(192);
        // Seven five-bit rows per glyph, top to bottom. Space has no authored pixels.
        private static readonly string[] Patterns =
        {
            "0E 11 11 1F 11 11 11", "1E 11 11 1E 11 11 1E", "0E 11 10 10 10 11 0E",
            "1E 11 11 11 11 11 1E", "1F 10 10 1E 10 10 1F", "1F 10 10 1E 10 10 10",
            "0F 10 10 13 11 11 0F", "11 11 11 1F 11 11 11", "1F 04 04 04 04 04 1F",
            "07 02 02 02 12 12 0C", "11 12 14 18 14 12 11", "10 10 10 10 10 10 1F",
            "11 1B 15 15 11 11 11", "11 19 15 13 11 11 11", "0E 11 11 11 11 11 0E",
            "1E 11 11 1E 10 10 10", "0E 11 11 11 15 12 0D", "1E 11 11 1E 14 12 11",
            "0F 10 10 0E 01 01 1E", "1F 04 04 04 04 04 04", "11 11 11 11 11 11 0E",
            "11 11 11 11 11 0A 04", "11 11 11 15 15 15 0A", "11 11 0A 04 0A 11 11",
            "11 11 0A 04 04 04 04", "1F 01 02 04 08 10 1F",
            "0E 11 13 15 19 11 0E", "04 0C 04 04 04 04 0E", "0E 11 01 02 04 08 1F",
            "1E 01 01 0E 01 01 1E", "02 06 0A 12 1F 02 02", "1F 10 10 1E 01 01 1E",
            "0E 10 10 1E 11 11 0E", "1F 01 02 04 08 08 08", "0E 11 11 0E 11 11 0E",
            "0E 11 11 0F 01 01 0E", "00 00 00 00 00 00 00", "00 00 00 1F 00 00 00",
            "01 02 02 04 08 08 10", "02 04 08 08 08 04 02", "08 04 02 02 02 04 08",
            "00 11 0A 04 0A 11 00", "000 0C6 129 111 129 0C6 000"
        };

        public static bool Supports(char character) => Characters.IndexOf(character) >= 0;

        public static int GetGlyphPixelWidth(char character)
        {
            if (!Supports(character))
            {
                throw new ArgumentException("Unsupported HUD glyph: " + character);
            }
            return character == '∞' ? InfinityGlyphWidth : GlyphWidth;
        }

        private static int GetGlyphAdvancePixels(char character)
        {
            return character == '∞' ? InfinityGlyphWidth + 1 : CellWidth;
        }

        public static Texture2D CreateAtlas()
        {
            int height = Mathf.CeilToInt(Characters.Length / (float)Columns) * CellHeight;
            var atlas = new Texture2D(Columns * AtlasCellWidth, height, TextureFormat.RGBA32, false, false)
            {
                name = "Rustline HUD Bitmap Font",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[atlas.width * atlas.height];
            Color32 white = new Color32(254, 254, 254, 255);
            for (int index = 0; index < Characters.Length; index++)
            {
                string[] rows = Patterns[index].Split(' ');
                char character = Characters[index];
                int glyphWidth = GetGlyphPixelWidth(character);
                int x = (index % Columns) * AtlasCellWidth;
                int yTop = (index / Columns) * CellHeight;
                for (int row = 0; row < GlyphHeight; row++)
                {
                    int bits = Convert.ToInt32(rows[row], 16);
                    for (int col = 0; col < glyphWidth; col++)
                    {
                        if ((bits & (1 << (glyphWidth - col - 1))) != 0)
                        {
                            pixels[(height - 1 - yTop - row) * atlas.width + x + col] = white;
                        }
                    }
                }
            }
            atlas.SetPixels32(pixels);
            atlas.Apply(false, false);
            return atlas;
        }

        /// <summary>Rebuilds one line mesh only when its displayed string changes.</summary>
        public static void BuildMesh(Mesh mesh, string value, int pixelScale)
        {
            mesh.Clear();
            if (string.IsNullOrEmpty(value)) return;

            int scale = Mathf.Max(1, pixelScale);
            int atlasHeight = Mathf.CeilToInt(Characters.Length / (float)Columns) * CellHeight;
            List<Vector3> vertices = Vertices;
            List<Vector2> uv = Uvs;
            List<int> triangles = Triangles;
            vertices.Clear();
            uv.Clear();
            triangles.Clear();
            float ppu = NativePixelPresentation.PixelsPerUnit;
            int cursorPixels = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                int glyph = Characters.IndexOf(character);
                if (glyph < 0) throw new ArgumentException("Unsupported HUD glyph: " + character);
                int glyphWidth = GetGlyphPixelWidth(character);
                float left = cursorPixels * scale / ppu;
                float right = left + glyphWidth * scale / ppu;
                float bottom = -GlyphHeight * scale / ppu;
                int first = vertices.Count;
                vertices.Add(new Vector3(left, 0f, 0f));
                vertices.Add(new Vector3(right, 0f, 0f));
                vertices.Add(new Vector3(left, bottom, 0f));
                vertices.Add(new Vector3(right, bottom, 0f));
                float u0 = (glyph % Columns) * AtlasCellWidth / (float)(Columns * AtlasCellWidth);
                float u1 = ((glyph % Columns) * AtlasCellWidth + glyphWidth) /
                    (float)(Columns * AtlasCellWidth);
                float v1 = 1f - (glyph / Columns) * CellHeight / (float)atlasHeight;
                float v0 = 1f - ((glyph / Columns) * CellHeight + GlyphHeight) / (float)atlasHeight;
                uv.Add(new Vector2(u0, v1)); uv.Add(new Vector2(u1, v1));
                uv.Add(new Vector2(u0, v0)); uv.Add(new Vector2(u1, v0));
                triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 1);
                triangles.Add(first + 1); triangles.Add(first + 2); triangles.Add(first + 3);
                cursorPixels += GetGlyphAdvancePixels(character);
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
