using System;
using UnityEngine;

namespace Rustline.Presentation
{
    public enum LongwatchMuzzleState2D
    {
        Idle = 0,
        Run = 1,
        Backpedal = 2,
        Crouch = 3,
    }

    public readonly struct LongwatchRenderedPose2D
    {
        public LongwatchRenderedPose2D(
            LongwatchMuzzleState2D state,
            int directionIndex,
            int authoredAngleDegrees,
            int frameIndex,
            bool facingLeft)
        {
            State = state;
            DirectionIndex = directionIndex;
            AuthoredAngleDegrees = authoredAngleDegrees;
            FrameIndex = frameIndex;
            FacingLeft = facingLeft;
        }

        public LongwatchMuzzleState2D State { get; }
        public int DirectionIndex { get; }
        public int AuthoredAngleDegrees { get; }
        public int FrameIndex { get; }
        public bool FacingLeft { get; }
    }

    [Serializable]
    public struct LongwatchMuzzleDirection2D
    {
        [SerializeField] private string suffix;
        [SerializeField] private int angleDegrees;
        [SerializeField] private bool supported;
        [SerializeField] private Vector2[] frameOffsetsPixels;

        public string Suffix => suffix;
        public int AngleDegrees => angleDegrees;
        public bool Supported => supported;
        public int FrameCount => frameOffsetsPixels?.Length ?? 0;

        public bool TryGetFrameOffset(int frameIndex, out Vector2 offsetPixels)
        {
            if (!supported || frameOffsetsPixels == null ||
                frameIndex < 0 || frameIndex >= frameOffsetsPixels.Length)
            {
                offsetPixels = default;
                return false;
            }

            offsetPixels = frameOffsetsPixels[frameIndex];
            return true;
        }
    }

    /// <summary>
    /// Compact runtime copy of the deterministic Longwatch muzzle JSON.
    /// The Editor setup owns generation; runtime performs only indexed lookups.
    /// </summary>
    public sealed class LongwatchMuzzleMetadata2D : ScriptableObject
    {
        public const int DirectionCount = 19;

        [SerializeField] private int schemaVersion;
        [SerializeField] private int generatorVersion;
        [SerializeField] private string weaponId;
        [SerializeField] private Vector2Int cellSizePixels;
        [SerializeField] private Vector2Int pivotPixels;
        [SerializeField] private LongwatchMuzzleDirection2D[] idleDirections =
            Array.Empty<LongwatchMuzzleDirection2D>();
        [SerializeField] private LongwatchMuzzleDirection2D[] runDirections =
            Array.Empty<LongwatchMuzzleDirection2D>();
        [SerializeField] private LongwatchMuzzleDirection2D[] backpedalDirections =
            Array.Empty<LongwatchMuzzleDirection2D>();
        [SerializeField] private LongwatchMuzzleDirection2D[] crouchDirections =
            Array.Empty<LongwatchMuzzleDirection2D>();

        public int SchemaVersion => schemaVersion;
        public int GeneratorVersion => generatorVersion;
        public string WeaponId => weaponId;
        public Vector2Int CellSizePixels => cellSizePixels;
        public Vector2Int PivotPixels => pivotPixels;

        public bool TryGetMuzzleOffset(
            LongwatchMuzzleState2D state,
            int directionIndex,
            int frameIndex,
            out Vector2 offsetPixels)
        {
            LongwatchMuzzleDirection2D[] directions = GetDirections(state);
            if (directions == null || directions.Length != DirectionCount ||
                directionIndex < 0 || directionIndex >= directions.Length)
            {
                offsetPixels = default;
                return false;
            }

            return directions[directionIndex].TryGetFrameOffset(frameIndex, out offsetPixels);
        }

        public LongwatchMuzzleDirection2D GetDirection(
            LongwatchMuzzleState2D state,
            int directionIndex)
        {
            LongwatchMuzzleDirection2D[] directions = GetDirections(state);
            if (directions == null || directionIndex < 0 || directionIndex >= directions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(directionIndex));
            }

            return directions[directionIndex];
        }

        public int GetSupportedPointCount()
        {
            return CountSupportedPoints(idleDirections) + CountSupportedPoints(runDirections) +
                   CountSupportedPoints(backpedalDirections) + CountSupportedPoints(crouchDirections);
        }

        private LongwatchMuzzleDirection2D[] GetDirections(LongwatchMuzzleState2D state)
        {
            switch (state)
            {
                case LongwatchMuzzleState2D.Idle: return idleDirections;
                case LongwatchMuzzleState2D.Run: return runDirections;
                case LongwatchMuzzleState2D.Backpedal: return backpedalDirections;
                case LongwatchMuzzleState2D.Crouch: return crouchDirections;
                default: return null;
            }
        }

        private static int CountSupportedPoints(LongwatchMuzzleDirection2D[] directions)
        {
            if (directions == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < directions.Length; index++)
            {
                if (directions[index].Supported)
                {
                    count += directions[index].FrameCount;
                }
            }

            return count;
        }
    }
}
