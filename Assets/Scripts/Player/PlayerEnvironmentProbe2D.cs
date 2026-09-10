using UnityEngine;

namespace Rustline.Gameplay.Player
{
    /// <summary>
    /// Art-derived contact contract for the approved 48x64 Wall Brace pose.
    /// Heights are measured from the physical player root after the Visual child's -4 px offset.
    /// </summary>
    public static class PlayerWallBraceContact2D
    {
        public const float PixelsPerUnit = 16f;
        public const float RequiredBottomYPixels = 14.5f;
        public const float RequiredTopYPixels = 44.5f;
        public const float RequiredWallPlaneXPixels = 9.5f;
        public const float SampleStepPixels = 5f;
        public const int SampleCount = 7;
        public const float WallPlaneTolerancePixels = 1f;

        public static float GetSampleHeightPixels(int sampleIndex)
        {
            return RequiredBottomYPixels +
                Mathf.Clamp(sampleIndex, 0, SampleCount - 1) * SampleStepPixels;
        }

        public static bool HasRequiredCoverage(
            Vector2 rootPosition,
            int side,
            float standingColliderWidth,
            float wallCheckDistance,
            float minimumWallNormalX,
            ContactFilter2D filter,
            RaycastHit2D[] hits)
        {
            if (side == 0 || standingColliderWidth <= 0f || wallCheckDistance <= 0f ||
                hits == null || hits.Length == 0)
            {
                return false;
            }

            side = side < 0 ? -1 : 1;
            Vector2 direction = Vector2.right * side;
            float maximumReach = standingColliderWidth * 0.5f + wallCheckDistance;
            float wallPlaneTolerance = WallPlaneTolerancePixels / PixelsPerUnit;
            float referenceWallX = 0f;

            for (int sampleIndex = 0; sampleIndex < SampleCount; sampleIndex++)
            {
                Vector2 origin = rootPosition + Vector2.up *
                    (GetSampleHeightPixels(sampleIndex) / PixelsPerUnit);
                int hitCount = Physics2D.Raycast(origin, direction, filter, hits, maximumReach);
                bool foundWall = false;
                float closestDistance = float.PositiveInfinity;
                float sampleWallX = 0f;
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    RaycastHit2D hit = hits[hitIndex];
                    if (hit.collider != null &&
                        -hit.normal.x * side >= minimumWallNormalX &&
                        hit.distance < closestDistance)
                    {
                        foundWall = true;
                        closestDistance = hit.distance;
                        sampleWallX = hit.point.x;
                    }
                }

                if (!foundWall)
                {
                    return false;
                }

                if (sampleIndex == 0)
                {
                    referenceWallX = sampleWallX;
                }
                else if (Mathf.Abs(sampleWallX - referenceWallX) > wallPlaneTolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct LedgeClimbCandidate2D
    {
        public LedgeClimbCandidate2D(
            int side,
            Vector2 ledgePoint,
            Vector2 captureRootPosition,
            Vector2 finalRootPosition)
        {
            Side = side;
            LedgePoint = ledgePoint;
            CaptureRootPosition = captureRootPosition;
            FinalRootPosition = finalRootPosition;
        }

        public int Side { get; }
        public Vector2 LedgePoint { get; }
        public float WallX => LedgePoint.x;
        public float TopY => LedgePoint.y;
        public Vector2 CaptureRootPosition { get; }
        public Vector2 FinalRootPosition { get; }
    }

    [RequireComponent(typeof(CapsuleCollider2D), typeof(PlayerGroundProbe2D))]
    public sealed class PlayerEnvironmentProbe2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        private readonly Collider2D[] _overlaps = new Collider2D[8];
        private CapsuleCollider2D _collider;
        private PlayerGroundProbe2D _groundProbe;
        private ContactFilter2D _filter;

        private void Awake()
        {
            _collider = GetComponent<CapsuleCollider2D>();
            _groundProbe = GetComponent<PlayerGroundProbe2D>();
            RebuildFilter();
        }

        private void OnValidate()
        {
            if (_groundProbe == null)
            {
                _groundProbe = GetComponent<PlayerGroundProbe2D>();
            }

            RebuildFilter();
        }

        public bool HasStandingClearance()
        {
            if (_collider == null || config == null || _collider.size.y >= config.StandingColliderSize.y)
            {
                return true;
            }

            float expansionDistance = config.StandingColliderSize.y - _collider.size.y;
            int hitCount = _collider.Cast(Vector2.up, _filter, _hits, expansionDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && hit.normal.y < 0f)
                {
                    return false;
                }
            }

            return true;
        }

        public int FindWallSide(float horizontalInput)
        {
            if (_collider == null || config == null || Mathf.Abs(horizontalInput) < config.InputDeadZone)
            {
                return 0;
            }

            int side = horizontalInput < 0f ? -1 : 1;
            int hitCount = _collider.Cast(Vector2.right * side, _filter, _hits, config.WallCheckDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && -hit.normal.x * side >= config.MinimumWallNormalX)
                {
                    return PlayerWallBraceContact2D.HasRequiredCoverage(
                        transform.position,
                        side,
                        config.StandingColliderSize.x,
                        config.WallCheckDistance,
                        config.MinimumWallNormalX,
                        _filter,
                        _hits)
                        ? side
                        : 0;
                }
            }

            return 0;
        }

        public bool TryFindLedgeClimb(int side, Vector2 rootPosition, out LedgeClimbCandidate2D candidate)
        {
            candidate = default;
            if (_collider == null || config == null || side == 0)
            {
                return false;
            }

            side = side < 0 ? -1 : 1;
            float tolerance = config.LedgeCaptureTolerance;
            float pixel = PlayerLedgeClimbMotion2D.SourcePixel;
            Vector2 expectedCorner = rootPosition + PlayerLedgeClimbMotion2D.GetExpectedContactOffset(side);

            Vector2 wallOrigin = expectedCorner - Vector2.right * side * tolerance - Vector2.up * pixel;
            int wallHitCount = Physics2D.Raycast(
                wallOrigin,
                Vector2.right * side,
                _filter,
                _hits,
                tolerance * 2f);
            bool foundWall = false;
            float wallX = 0f;
            for (int index = 0; index < wallHitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && -hit.normal.x * side >= config.MinimumWallNormalX)
                {
                    wallX = hit.point.x;
                    foundWall = true;
                    break;
                }
            }

            if (!foundWall)
            {
                return false;
            }

            Vector2 topOrigin = new Vector2(
                wallX + side * pixel,
                expectedCorner.y + tolerance);
            int topHitCount = Physics2D.Raycast(
                topOrigin,
                Vector2.down,
                _filter,
                _hits,
                tolerance * 2f);
            bool foundTop = false;
            float topY = 0f;
            for (int index = 0; index < topHitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && hit.normal.y >= config.MinimumGroundNormalY)
                {
                    topY = hit.point.y;
                    foundTop = true;
                    break;
                }
            }

            Vector2 ledgePoint = new Vector2(wallX, topY);
            if (!foundTop || Mathf.Abs(ledgePoint.x - expectedCorner.x) > tolerance ||
                Mathf.Abs(ledgePoint.y - expectedCorner.y) > tolerance)
            {
                return false;
            }

            Vector2 contactOffset = PlayerLedgeClimbMotion2D.GetExpectedContactOffset(side);
            Vector2 captureRoot = ledgePoint - contactOffset;
            Vector2 finalRoot = new Vector2(
                wallX + side * (config.StandingColliderSize.x * 0.5f + pixel),
                topY + pixel);
            if (!HasStandingClearanceAt(finalRoot) || !HasGroundSupportAt(finalRoot))
            {
                return false;
            }

            candidate = new LedgeClimbCandidate2D(side, ledgePoint, captureRoot, finalRoot);
            return true;
        }

        public bool HasStandingClearanceAt(Vector2 rootPosition)
        {
            if (config == null)
            {
                return false;
            }

            Vector2 center = rootPosition + config.StandingColliderOffset;
            return Physics2D.OverlapCapsule(
                center,
                config.StandingColliderSize,
                CapsuleDirection2D.Vertical,
                0f,
                _filter,
                _overlaps) == 0;
        }

        public bool HasGroundSupportAt(Vector2 rootPosition)
        {
            if (config == null)
            {
                return false;
            }

            Vector2 center = rootPosition + config.StandingColliderOffset;
            int hitCount = Physics2D.CapsuleCast(
                center,
                config.StandingColliderSize,
                CapsuleDirection2D.Vertical,
                0f,
                Vector2.down,
                _filter,
                _hits,
                config.GroundCheckDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && hit.normal.y >= config.MinimumGroundNormalY)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildFilter()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundProbe != null ? _groundProbe.GroundLayers : 0,
                useTriggers = false,
            };
        }
    }
}
