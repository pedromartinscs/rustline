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
        public const float AuthoredPhaseDuration = 0.4f;
        public const float SettleDuration = 0.16f;
        public const float TotalDuration = AuthoredPhaseDuration + SettleDuration;
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
            Vector2 finalRootPosition,
            int side,
            float elapsed)
        {
            int frame = GetFrameIndex(elapsed);
            Vector2 authoredPosition = captureRootPosition + GetRootOffset(frame, side);
            if (elapsed <= AuthoredPhaseDuration)
            {
                return authoredPosition;
            }

            float progress = (elapsed - AuthoredPhaseDuration) / SettleDuration;
            return Vector2.Lerp(authoredPosition, finalRootPosition, EaseOutCubic(progress));
        }

        public static float EaseOutCubic(float normalizedTime)
        {
            float inverse = 1f - Mathf.Clamp01(normalizedTime);
            return 1f - inverse * inverse * inverse;
        }
    }
}
