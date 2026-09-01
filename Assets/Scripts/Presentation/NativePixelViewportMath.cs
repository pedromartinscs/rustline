using System;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Immutable result of Rustline's native-pixel viewport calculation.
    /// All dimensions and offsets are integral physical/logical pixels.
    /// </summary>
    public readonly struct NativePixelViewport
    {
        public NativePixelViewport(
            int physicalWidth,
            int physicalHeight,
            int integerScale,
            int logicalWidth,
            int logicalHeight,
            int outputWidth,
            int outputHeight,
            int outputOffsetX,
            int outputOffsetY)
        {
            PhysicalWidth = physicalWidth;
            PhysicalHeight = physicalHeight;
            IntegerScale = integerScale;
            LogicalWidth = logicalWidth;
            LogicalHeight = logicalHeight;
            OutputWidth = outputWidth;
            OutputHeight = outputHeight;
            OutputOffsetX = outputOffsetX;
            OutputOffsetY = outputOffsetY;
        }

        public int PhysicalWidth { get; }
        public int PhysicalHeight { get; }
        public int IntegerScale { get; }
        public int LogicalWidth { get; }
        public int LogicalHeight { get; }
        public int OutputWidth { get; }
        public int OutputHeight { get; }
        public int OutputOffsetX { get; }
        public int OutputOffsetY { get; }
        public RectInt OutputRect => new RectInt(OutputOffsetX, OutputOffsetY, OutputWidth, OutputHeight);
    }

    /// <summary>
    /// Pure viewport math shared by runtime presentation, diagnostics, and EditMode tests.
    /// </summary>
    public static class NativePixelViewportMath
    {
        public const int MaximumLogicalDimension = 1072;

        public static NativePixelViewport Calculate(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(screenWidth), "Screen width must be positive.");
            }

            if (screenHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(screenHeight), "Screen height must be positive.");
            }

            int integerScale = Math.Max(
                1,
                Math.Min(screenWidth / MaximumLogicalDimension, screenHeight / MaximumLogicalDimension));
            int logicalWidth = Math.Min(MaximumLogicalDimension, screenWidth / integerScale);
            int logicalHeight = Math.Min(MaximumLogicalDimension, screenHeight / integerScale);
            int outputWidth = logicalWidth * integerScale;
            int outputHeight = logicalHeight * integerScale;

            return new NativePixelViewport(
                screenWidth,
                screenHeight,
                integerScale,
                logicalWidth,
                logicalHeight,
                outputWidth,
                outputHeight,
                (screenWidth - outputWidth) / 2,
                (screenHeight - outputHeight) / 2);
        }
    }
}
