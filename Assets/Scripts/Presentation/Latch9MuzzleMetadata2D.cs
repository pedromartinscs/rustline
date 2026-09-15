using System;
using UnityEngine;

namespace Rustline.Presentation
{
    public enum Latch9MuzzleState2D
    {
        Idle = 0,
        Run = 1,
        Backpedal = 2,
        Crouch = 3,
        Fall = 4,
    }

    public readonly struct Latch9RenderedPose2D
    {
        public Latch9RenderedPose2D(
            Latch9MuzzleState2D state,
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

        public Latch9MuzzleState2D State { get; }
        public int DirectionIndex { get; }
        public int AuthoredAngleDegrees { get; }
        public int FrameIndex { get; }
        public bool FacingLeft { get; }
    }

    [Serializable]
    public struct Latch9MuzzleDirection2D
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
    /// Compact runtime copy of the deterministic Latch-9 muzzle metadata generated
    /// from ArtSource. The Editor setup owns generation; runtime performs indexed lookups only.
    /// </summary>
    public sealed class Latch9MuzzleMetadata2D : ScriptableObject
    {
        public const int DirectionCount = 19;

        [SerializeField] private int schemaVersion;
        [SerializeField] private int generatorVersion;
        [SerializeField] private string weaponId;
        [SerializeField] private Vector2Int cellSizePixels;
        [SerializeField] private Vector2Int pivotPixels;
        [SerializeField] private Latch9MuzzleDirection2D[] idleDirections =
            Array.Empty<Latch9MuzzleDirection2D>();
        [SerializeField] private Latch9MuzzleDirection2D[] runDirections =
            Array.Empty<Latch9MuzzleDirection2D>();
        [SerializeField] private Latch9MuzzleDirection2D[] backpedalDirections =
            Array.Empty<Latch9MuzzleDirection2D>();
        [SerializeField] private Latch9MuzzleDirection2D[] crouchDirections =
            Array.Empty<Latch9MuzzleDirection2D>();
        [SerializeField] private Latch9MuzzleDirection2D[] fallDirections =
            Array.Empty<Latch9MuzzleDirection2D>();

        public int SchemaVersion => schemaVersion;
        public int GeneratorVersion => generatorVersion;
        public string WeaponId => weaponId;
        public Vector2Int CellSizePixels => cellSizePixels;
        public Vector2Int PivotPixels => pivotPixels;

        public bool TryGetMuzzleOffset(
            Latch9MuzzleState2D state,
            int directionIndex,
            int frameIndex,
            out Vector2 offsetPixels)
        {
            Latch9MuzzleDirection2D[] directions = GetDirections(state);
            if (directions == null || directions.Length != DirectionCount ||
                directionIndex < 0 || directionIndex >= directions.Length)
            {
                offsetPixels = default;
                return false;
            }

            return directions[directionIndex].TryGetFrameOffset(frameIndex, out offsetPixels);
        }

        public Latch9MuzzleDirection2D GetDirection(
            Latch9MuzzleState2D state,
            int directionIndex)
        {
            Latch9MuzzleDirection2D[] directions = GetDirections(state);
            if (directions == null || directionIndex < 0 || directionIndex >= directions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(directionIndex));
            }

            return directions[directionIndex];
        }

        public int GetSupportedPointCount()
        {
            return CountSupportedPoints(idleDirections) + CountSupportedPoints(runDirections) +
                   CountSupportedPoints(backpedalDirections) + CountSupportedPoints(crouchDirections) +
                   CountSupportedPoints(fallDirections);
        }

        private Latch9MuzzleDirection2D[] GetDirections(Latch9MuzzleState2D state)
        {
            switch (state)
            {
                case Latch9MuzzleState2D.Idle: return idleDirections;
                case Latch9MuzzleState2D.Run: return runDirections;
                case Latch9MuzzleState2D.Backpedal: return backpedalDirections;
                case Latch9MuzzleState2D.Crouch: return crouchDirections;
                case Latch9MuzzleState2D.Fall: return fallDirections;
                default: return null;
            }
        }

        private static int CountSupportedPoints(Latch9MuzzleDirection2D[] directions)
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
