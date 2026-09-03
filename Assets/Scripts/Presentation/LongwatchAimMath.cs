using UnityEngine;

namespace Rustline.Presentation
{
    public readonly struct LongwatchAimSelection
    {
        public LongwatchAimSelection(float continuousAngleDegrees, int authoredAngleDegrees, bool flipX)
        {
            ContinuousAngleDegrees = continuousAngleDegrees;
            AuthoredAngleDegrees = authoredAngleDegrees;
            FlipX = flipX;
        }

        public float ContinuousAngleDegrees { get; }
        public int AuthoredAngleDegrees { get; }
        public bool FlipX { get; }
        public int DirectionIndex => (90 - AuthoredAngleDegrees) / 10;

        public static LongwatchAimSelection Default => new LongwatchAimSelection(0f, 0, false);
    }

    /// <summary>
    /// Pure continuous-aim normalization and authored ten-degree pose selection.
    /// </summary>
    public static class LongwatchAimMath
    {
        public const float MinimumAimMagnitudeSquared = 0.000001f;

        public static bool TrySelect(
            Vector2 aimVector,
            bool hasPreviousSelection,
            LongwatchAimSelection previousSelection,
            out LongwatchAimSelection selection)
        {
            if (aimVector.sqrMagnitude < MinimumAimMagnitudeSquared)
            {
                selection = hasPreviousSelection ? previousSelection : LongwatchAimSelection.Default;
                return false;
            }

            bool flipX;
            if (aimVector.x < 0f)
            {
                flipX = true;
            }
            else if (aimVector.x > 0f)
            {
                flipX = false;
            }
            else
            {
                // Exact vertical aim belongs to both mirrored hemispheres. Retaining the
                // previous side avoids unnecessary facing flicker through the vertical axis.
                flipX = hasPreviousSelection && previousSelection.FlipX;
            }

            float continuousAngle = Mathf.Atan2(aimVector.y, Mathf.Abs(aimVector.x)) * Mathf.Rad2Deg;
            int authoredAngle = QuantizeToNearestTen(continuousAngle);
            selection = new LongwatchAimSelection(continuousAngle, authoredAngle, flipX);
            return true;
        }

        public static int QuantizeToNearestTen(float angleDegrees)
        {
            float clamped = Mathf.Clamp(angleDegrees, -90f, 90f);
            int magnitude = Mathf.FloorToInt(Mathf.Abs(clamped) / 10f + 0.5f) * 10;
            return clamped < 0f ? -magnitude : magnitude;
        }
    }
}
