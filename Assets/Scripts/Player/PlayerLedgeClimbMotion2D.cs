using UnityEngine;

namespace Rustline.Gameplay.Player
{
    /// <summary>
    /// Fixed authored motion contract for the committed five-frame ledge climb.
    /// Values are source pixels converted at the canonical 16 PPU.
    /// </summary>
    public static class PlayerLedgeClimbMotion2D
    {
        public const int FrameCount = 5;
        public const float PixelsPerUnit = 16f;
        public const float SourcePixel = 1f / PixelsPerUnit;
        public const float FrameDuration = 0.1f;
        // Five authored frames at 10 fps. Frame 4 is held at its calibrated ledge-contact
        // position until the state ends; the physical handoff to standing happens atomically
        // at the same boundary where presentation leaves LedgeClimb.
        public const float TotalDuration = FrameCount * FrameDuration;
        public const float ContactOffsetXPixels = 16.5f;
        public const float ContactOffsetYPixels = 40.5f;

        private static readonly Vector2[] RightSideRootOffsetsPixels =
        {
            new Vector2(0f, 0f),
            new Vector2(8f, 8f),
            new Vector2(14f, 14f),
            new Vector2(18f, 17f),
            new Vector2(19f, 19f),
        };

        public static Vector2 GetExpectedContactOffset(int side)
        {
            return new Vector2(
                Mathf.Sign(side) * ContactOffsetXPixels * SourcePixel,
                ContactOffsetYPixels * SourcePixel);
        }

        public static int GetFrameIndex(float elapsed)
        {
            int frame = Mathf.FloorToInt(Mathf.Max(0f, elapsed) / FrameDuration + 0.00001f);
            return Mathf.Clamp(frame, 0, FrameCount - 1);
        }

        public static Vector2 GetRootOffset(int frameIndex, int side)
        {
            Vector2 pixels = RightSideRootOffsetsPixels[Mathf.Clamp(frameIndex, 0, FrameCount - 1)];
            return new Vector2(Mathf.Sign(side) * pixels.x, pixels.y) * SourcePixel;
        }

        public static Vector2 GetPosition(
            Vector2 captureRootPosition,
            int side,
            float elapsed)
        {
            int frame = GetFrameIndex(elapsed);
            return captureRootPosition + GetRootOffset(frame, side);
        }
    }
}
