using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Auditable runtime copy of Rustline Canonical 28 and the five-level penumbra LUT.
    /// LUT entries are palette indices, so they cannot synthesize non-canonical colors.
    /// </summary>
    public static class RustlinePalette
    {
        public const int ColorCount = 28;
        public const int DarknessLevelCount = 5;
        public const int DeepSpaceIndex = 0;

        private static readonly Color32[] Colors =
        {
            Hex(0x01, 0x02, 0x0B), // Deep Space
            Hex(0x0D, 0x17, 0x2C), // Deep Navy
            Hex(0x22, 0x37, 0x4D), // Shadow
            Hex(0x35, 0x40, 0x5A), // Steel Shadow
            Hex(0x42, 0x59, 0x70), // Dark Metal
            Hex(0x68, 0x7C, 0x90), // Steel
            Hex(0xC9, 0xBB, 0xB1), // Light Metal
            Hex(0xBE, 0x99, 0x7E), // Concrete
            Hex(0x46, 0x24, 0x1E), // Warm Shadow
            Hex(0x7C, 0x33, 0x12), // Rust Mid
            Hex(0xB0, 0x46, 0x1C), // Rust Dark
            Hex(0xED, 0x75, 0x27), // Rust Orange
            Hex(0xFD, 0xD0, 0x45), // Hazard Yellow
            Hex(0xFB, 0xAB, 0x29), // Warning Orange
            Hex(0xF4, 0x3C, 0x2C), // Red
            Hex(0xFB, 0xD3, 0xB5), // Skin Beige
            Hex(0xC8, 0x9F, 0x7E), // Fabric Tan
            Hex(0x99, 0x6F, 0x56), // Fabric Brown
            Hex(0x02, 0x86, 0x9A), // Cyan Dark
            Hex(0x0B, 0xD3, 0xD6), // Cyan
            Hex(0x20, 0xED, 0xE5), // Neon Cyan
            Hex(0x56, 0xB7, 0x53), // Green
            Hex(0x8C, 0x35, 0xD0), // Violet
            Hex(0xFE, 0xD4, 0x37), // Muzzle Yellow
            Hex(0xFE, 0xFE, 0xFE), // Muzzle White
            Hex(0xB0, 0xAA, 0xAB), // Smoke
            Hex(0x99, 0x0E, 0x0E), // Blood
            Hex(0x15, 0xD8, 0xF2), // UI Blue
        };

        // Rows are source colors in canonical order. Columns are darkness levels 0..4.
        // Level 0 is identity. Level 4 is always Deep Space.
        private static readonly byte[] DarknessIndices =
        {
             0,  0,  0,  0, 0, // Deep Space
             1,  1,  1,  0, 0, // Deep Navy
             2,  2,  1,  1, 0, // Shadow
             3,  3,  2,  1, 0, // Steel Shadow
             4,  4,  3,  2, 0, // Dark Metal
             5,  4,  3,  2, 0, // Steel
             6,  5,  4,  2, 0, // Light Metal
             7,  5,  4,  2, 0, // Concrete
             8,  8,  2,  1, 0, // Warm Shadow
             9,  8,  2,  1, 0, // Rust Mid
            10,  9,  8,  2, 0, // Rust Dark
            11, 10,  9,  8, 0, // Rust Orange
            12, 13, 10,  8, 0, // Hazard Yellow
            13, 11,  9,  8, 0, // Warning Orange
            14, 10,  9,  8, 0, // Red
            15, 16, 17,  8, 0, // Skin Beige
            16, 17,  8,  2, 0, // Fabric Tan
            17,  8,  2,  1, 0, // Fabric Brown
            18, 18,  2,  1, 0, // Cyan Dark
            19, 18,  2,  1, 0, // Cyan
            20, 19, 18,  2, 0, // Neon Cyan
            21, 21,  4,  1, 0, // Green
            22, 22,  3,  1, 0, // Violet
            23, 12, 13,  8, 0, // Muzzle Yellow
            24,  6,  5,  2, 0, // Muzzle White
            25,  5,  4,  2, 0, // Smoke
            26, 26,  8,  1, 0, // Blood
            27, 19, 18,  2, 0, // UI Blue
        };

        public static IReadOnlyList<Color32> CanonicalColors => Colors;
        public static Color32 DeepSpace => Colors[DeepSpaceIndex];
        public static Color DeepSpaceCameraClear => (Color)DeepSpace;

        // Legacy name kept because the deterministic MovementLab setup already references it.
        // Camera clear colors in this pipeline must use the authored canonical display value,
        // not Color.linear; converting #01020B first makes the surround effectively black.
        public static Color DeepSpaceLinear => DeepSpaceCameraClear;

        public static Color32 GetColor(int paletteIndex)
        {
            if ((uint)paletteIndex >= ColorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(paletteIndex));
            }

            return Colors[paletteIndex];
        }

        public static int GetDarknessIndex(int sourceIndex, int level)
        {
            if ((uint)sourceIndex >= ColorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if ((uint)level >= DarknessLevelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return DarknessIndices[sourceIndex * DarknessLevelCount + level];
        }

        public static Color32 GetDarkenedColor(int sourceIndex, int level)
        {
            return Colors[GetDarknessIndex(sourceIndex, level)];
        }

        public static bool IsCanonical(Color32 color)
        {
            for (int index = 0; index < Colors.Length; index++)
            {
                if (Colors[index].Equals(color))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void CopyLinearShaderData(Vector4[] palette, Vector4[] darknessLut)
        {
            if (palette == null || palette.Length != ColorCount)
            {
                throw new ArgumentException("Palette shader buffer has the wrong length.", nameof(palette));
            }

            if (darknessLut == null || darknessLut.Length != ColorCount * DarknessLevelCount)
            {
                throw new ArgumentException("Darkness LUT shader buffer has the wrong length.", nameof(darknessLut));
            }

            for (int sourceIndex = 0; sourceIndex < ColorCount; sourceIndex++)
            {
                Color sourceLinear = ((Color)Colors[sourceIndex]).linear;
                palette[sourceIndex] = sourceLinear;
                for (int level = 0; level < DarknessLevelCount; level++)
                {
                    Color mappedLinear = ((Color)GetDarkenedColor(sourceIndex, level)).linear;
                    darknessLut[sourceIndex * DarknessLevelCount + level] = mappedLinear;
                }
            }
        }

        private static Color32 Hex(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, byte.MaxValue);
        }
    }
}
