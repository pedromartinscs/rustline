using Rustline.Presentation;
using Unity.Profiling;
using UnityEngine;

namespace Rustline.Gameplay.Player
{
    public static class PlayerAimMath
    {
        public const float VerticalHemisphereHysteresisDegrees = 5f;
        public const float MinimumAimMagnitudeSquared = 0.000001f;

        private static readonly float VerticalHemisphereHysteresisXThreshold = Mathf.Sin(
            VerticalHemisphereHysteresisDegrees * Mathf.Deg2Rad);

        public static bool TryResolve(
            Vector2 aimVector,
            bool hasPreviousFacing,
            bool previousFacingLeft,
            out Vector2 continuousDirection,
            out bool facingLeft)
        {
            if (!float.IsFinite(aimVector.x) || !float.IsFinite(aimVector.y) ||
                aimVector.sqrMagnitude < MinimumAimMagnitudeSquared)
            {
                continuousDirection = Vector2.zero;
                facingLeft = hasPreviousFacing && previousFacingLeft;
                return false;
            }

            continuousDirection = aimVector.normalized;
            if (Mathf.Abs(continuousDirection.x) <= VerticalHemisphereHysteresisXThreshold)
            {
                facingLeft = hasPreviousFacing && previousFacingLeft;
            }
            else
            {
                facingLeft = continuousDirection.x < 0f;
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

        private static readonly ProfilerMarker AimMarker = new ProfilerMarker("Rustline.Player.Aim");

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private NativePixelPresentation nativePixelPresentation;

        private Vector2 _continuousAimDirection = Vector2.right;
        private bool _hasValidAim;
        private bool _facingLeft;
        private uint _aimRevision;
        private bool _hasRefreshInputs;
        private PlayerInputReader _lastInput;
        private NativePixelPresentation _lastPresentation;
        private Camera _lastWorldCamera;
        private Vector2 _lastPointerScreenPosition;
        private Vector3 _lastAimOriginWorld;
        private Vector3 _lastCameraPosition;
        private Quaternion _lastCameraRotation;
        private float _lastOrthographicSize;
        private float _lastCameraAspect;
        private Rect _lastCameraRect;
        private NativePixelViewport _lastViewport;

        public PlayerInputReader Input => input;
        public Transform AimOrigin => aimOrigin;
        public NativePixelPresentation NativePixelPresentation => nativePixelPresentation;
        public Vector2 ContinuousAimDirection => _continuousAimDirection;
        public bool HasValidAim => _hasValidAim;
        public bool FacingLeft => _facingLeft;
        public uint AimRevision => _aimRevision;
        public Vector3 AimOriginWorld => aimOrigin != null
            ? aimOrigin.position
            : transform.position + Vector3.up * AimOriginOffsetWorldUnits;

        private void Update()
        {
            RefreshAim();
        }

        private void OnEnable()
        {
            InvalidateRefreshInputs();
        }

        public void RefreshAim()
        {
            using (AimMarker.Auto())
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

                Camera worldCamera = nativePixelPresentation.WorldCamera;
                Vector2 pointerScreenPosition = input.PointerScreenPosition;
                Vector3 originWorld = AimOriginWorld;
                Transform cameraTransform = worldCamera.transform;
                Vector3 cameraPosition = cameraTransform.position;
                Quaternion cameraRotation = cameraTransform.rotation;
                float orthographicSize = worldCamera.orthographicSize;
                float cameraAspect = worldCamera.aspect;
                Rect cameraRect = worldCamera.rect;

                if (RefreshInputsMatch(
                        input,
                        nativePixelPresentation,
                        worldCamera,
                        pointerScreenPosition,
                        originWorld,
                        cameraPosition,
                        cameraRotation,
                        orthographicSize,
                        cameraAspect,
                        cameraRect,
                        viewport))
                {
                    return;
                }

                Vector2 viewportPosition = NativePixelViewportMath.PhysicalToLogicalViewport(
                    pointerScreenPosition,
                    viewport);
                float originViewportDepth = worldCamera.WorldToViewportPoint(originWorld).z;
                Vector3 pointerWorld = worldCamera.ViewportToWorldPoint(
                    new Vector3(viewportPosition.x, viewportPosition.y, originViewportDepth));
                ApplyWorldAimVector((Vector2)(pointerWorld - originWorld));
                StoreRefreshInputs(
                    input,
                    nativePixelPresentation,
                    worldCamera,
                    pointerScreenPosition,
                    originWorld,
                    cameraPosition,
                    cameraRotation,
                    orthographicSize,
                    cameraAspect,
                    cameraRect,
                    viewport);
            }
        }

        public bool ApplyWorldAimVector(Vector2 aimVector)
        {
            InvalidateRefreshInputs();
            if (!PlayerAimMath.TryResolve(
                    aimVector,
                    _hasValidAim,
                    _facingLeft,
                    out Vector2 direction,
                    out bool facingLeft))
            {
                return false;
            }

            bool changed = !_hasValidAim ||
                           !direction.Equals(_continuousAimDirection) ||
                           facingLeft != _facingLeft;
            _continuousAimDirection = direction;
            _facingLeft = facingLeft;
            _hasValidAim = true;
            if (changed)
            {
                unchecked
                {
                    _aimRevision++;
                }
            }

            return true;
        }

        private bool RefreshInputsMatch(
            PlayerInputReader currentInput,
            NativePixelPresentation currentPresentation,
            Camera currentWorldCamera,
            Vector2 pointerScreenPosition,
            Vector3 originWorld,
            Vector3 cameraPosition,
            Quaternion cameraRotation,
            float orthographicSize,
            float cameraAspect,
            Rect cameraRect,
            NativePixelViewport viewport)
        {
            return _hasRefreshInputs &&
                   _lastInput == currentInput &&
                   _lastPresentation == currentPresentation &&
                   _lastWorldCamera == currentWorldCamera &&
                   _lastPointerScreenPosition.Equals(pointerScreenPosition) &&
                   _lastAimOriginWorld.Equals(originWorld) &&
                   _lastCameraPosition.Equals(cameraPosition) &&
                   _lastCameraRotation.Equals(cameraRotation) &&
                   _lastOrthographicSize == orthographicSize &&
                   _lastCameraAspect == cameraAspect &&
                   _lastCameraRect.Equals(cameraRect) &&
                   ViewportsMatch(_lastViewport, viewport);
        }

        private void StoreRefreshInputs(
            PlayerInputReader currentInput,
            NativePixelPresentation currentPresentation,
            Camera currentWorldCamera,
            Vector2 pointerScreenPosition,
            Vector3 originWorld,
            Vector3 cameraPosition,
            Quaternion cameraRotation,
            float orthographicSize,
            float cameraAspect,
            Rect cameraRect,
            NativePixelViewport viewport)
        {
            _lastInput = currentInput;
            _lastPresentation = currentPresentation;
            _lastWorldCamera = currentWorldCamera;
            _lastPointerScreenPosition = pointerScreenPosition;
            _lastAimOriginWorld = originWorld;
            _lastCameraPosition = cameraPosition;
            _lastCameraRotation = cameraRotation;
            _lastOrthographicSize = orthographicSize;
            _lastCameraAspect = cameraAspect;
            _lastCameraRect = cameraRect;
            _lastViewport = viewport;
            _hasRefreshInputs = true;
        }

        private void InvalidateRefreshInputs()
        {
            _hasRefreshInputs = false;
        }

        private static bool ViewportsMatch(NativePixelViewport left, NativePixelViewport right)
        {
            return left.PhysicalWidth == right.PhysicalWidth &&
                   left.PhysicalHeight == right.PhysicalHeight &&
                   left.IntegerScale == right.IntegerScale &&
                   left.LogicalWidth == right.LogicalWidth &&
                   left.LogicalHeight == right.LogicalHeight &&
                   left.OutputWidth == right.OutputWidth &&
                   left.OutputHeight == right.OutputHeight &&
                   left.OutputOffsetX == right.OutputOffsetX &&
                   left.OutputOffsetY == right.OutputOffsetY;
        }
    }
}
