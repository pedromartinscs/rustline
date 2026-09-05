using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Player
{
    public static class PlayerAimMath
    {
        public const float VerticalHemisphereHysteresisDegrees = 5f;
        public const float MinimumAimMagnitudeSquared = 0.000001f;

        public static bool TryResolve(
            Vector2 aimVector,
            bool hasPreviousFacing,
            bool previousFlipX,
            out Vector2 continuousDirection,
            out bool facingFlipX)
        {
            if (!float.IsFinite(aimVector.x) || !float.IsFinite(aimVector.y) ||
                aimVector.sqrMagnitude < MinimumAimMagnitudeSquared)
            {
                continuousDirection = Vector2.zero;
                facingFlipX = hasPreviousFacing && previousFlipX;
                return false;
            }

            continuousDirection = aimVector.normalized;
            float verticalZoneHalfWidth = Mathf.Sin(
                VerticalHemisphereHysteresisDegrees * Mathf.Deg2Rad);
            if (Mathf.Abs(continuousDirection.x) <= verticalZoneHalfWidth)
            {
                facingFlipX = hasPreviousFacing && previousFlipX;
            }
            else
            {
                facingFlipX = continuousDirection.x < 0f;
            }

            return true;
        }
    }

    /// <summary>
    /// Weapon-independent source of continuous world-space player aim and stable facing.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerAim2D : MonoBehaviour
    {
        public const float AimOriginOffsetSourcePixels = 38f;
        public const float SourcePixelsPerUnit = 16f;
        public const float AimOriginOffsetWorldUnits = AimOriginOffsetSourcePixels / SourcePixelsPerUnit;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private NativePixelPresentation nativePixelPresentation;

        private Vector2 _continuousAimDirection = Vector2.right;
        private bool _hasValidAim;
        private bool _facingFlipX;

        public PlayerInputReader Input => input;
        public Transform AimOrigin => aimOrigin;
        public NativePixelPresentation NativePixelPresentation => nativePixelPresentation;
        public Vector2 ContinuousAimDirection => _continuousAimDirection;
        public bool HasValidAim => _hasValidAim;
        public bool FacingFlipX => _facingFlipX;
        public Vector3 AimOriginWorld => aimOrigin != null
            ? aimOrigin.position
            : transform.position + Vector3.up * AimOriginOffsetWorldUnits;

        private void Update()
        {
            RefreshAim();
        }

        public void RefreshAim()
        {
            if (input == null || nativePixelPresentation == null ||
                nativePixelPresentation.WorldCamera == null)
            {
                return;
            }

            NativePixelViewport viewport = nativePixelPresentation.Viewport;
            if (viewport.IntegerScale <= 0 || viewport.LogicalWidth <= 0 || viewport.LogicalHeight <= 0)
            {
                return;
            }

            Vector2 viewportPosition = NativePixelViewportMath.PhysicalToLogicalViewport(
                input.PointerScreenPosition,
                viewport);
            Camera worldCamera = nativePixelPresentation.WorldCamera;
            Vector3 originWorld = AimOriginWorld;
            float originViewportDepth = worldCamera.WorldToViewportPoint(originWorld).z;
            Vector3 pointerWorld = worldCamera.ViewportToWorldPoint(
                new Vector3(viewportPosition.x, viewportPosition.y, originViewportDepth));
            ApplyWorldAimVector((Vector2)(pointerWorld - originWorld));
        }

        public bool ApplyWorldAimVector(Vector2 aimVector)
        {
            if (!PlayerAimMath.TryResolve(
                    aimVector,
                    _hasValidAim,
                    _facingFlipX,
                    out Vector2 direction,
                    out bool flipX))
            {
                return false;
            }

            _continuousAimDirection = direction;
            _facingFlipX = flipX;
            _hasValidAim = true;
            return true;
        }
    }
}
