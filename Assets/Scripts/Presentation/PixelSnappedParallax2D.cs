using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Horizontal, pixel-snapped backdrop parallax for seamless repeating source art.
    /// The World Camera is already snapped by PixelCameraFollow2D; this component runs later,
    /// follows a configured fraction of camera X, follows camera Y exactly, recenters itself by
    /// whole tile widths when necessary, then snaps its final transform to the same source-pixel grid.
    /// </summary>
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class PixelSnappedParallax2D : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float horizontalFollow = 0.94f;
        [SerializeField, Min(1)] private int tileWidthSourcePixels = 2160;
        [SerializeField] private int horizontalPhaseSourcePixels;
        [SerializeField] private int verticalScreenOffsetSourcePixels;
        [SerializeField, Min(1)] private int pixelsPerUnit = 16;

        private Transform _cameraTransform;
        private float _layerZ;

        public float HorizontalFollow => horizontalFollow;
        public int TileWidthSourcePixels => tileWidthSourcePixels;
        public int HorizontalPhaseSourcePixels => horizontalPhaseSourcePixels;
        public int VerticalScreenOffsetSourcePixels => verticalScreenOffsetSourcePixels;
        public int PixelsPerUnit => pixelsPerUnit;
        public float TileWidthWorldUnits => tileWidthSourcePixels / (float)pixelsPerUnit;

        private void OnEnable()
        {
            _cameraTransform = null;
            _layerZ = transform.position.z;
            TryResolveCamera();
        }

        private void LateUpdate()
        {
            if (!TryResolveCamera())
            {
                return;
            }

            Vector3 cameraPosition = _cameraTransform.position;
            float scale = pixelsPerUnit;
            float tileWidth = TileWidthWorldUnits;
            float phaseOffset = horizontalPhaseSourcePixels / scale;
            float verticalOffset = verticalScreenOffsetSourcePixels / scale;

            // Absolute world-phase parallax keeps the pattern deterministic regardless of where the
            // component was enabled. The managed root is then wrapped around the camera by exact
            // tile-width multiples. Because the source is horizontally seamless, that wrap is
            // visually identical while preventing the finite three-segment strip from drifting away.
            float rawParallaxX = cameraPosition.x * horizontalFollow + phaseOffset;
            float relativeX = rawParallaxX - cameraPosition.x;
            float wrappedRelativeX = WrapCentered(relativeX, tileWidth);
            float desiredX = cameraPosition.x + wrappedRelativeX;

            // Horizontal far parallax is deliberately screen-locked vertically. Camera climbs do
            // not make this plane float up/down inside the viewport; the future Vertical Depth
            // Backdrop owns long-range altitude communication instead.
            float desiredY = cameraPosition.y + verticalOffset;

            Vector3 snappedPosition = new Vector3(
                Mathf.Round(desiredX * scale) / scale,
                Mathf.Round(desiredY * scale) / scale,
                _layerZ);

            if (!transform.position.Equals(snappedPosition))
            {
                transform.position = snappedPosition;
            }
        }

        public void ConfigureHorizontalLoop(
            float horizontalFollowFactor,
            int sourceTileWidthPixels,
            int sourcePixelsPerUnit,
            int horizontalPhasePixels = 0,
            int verticalScreenOffsetPixels = 0)
        {
            horizontalFollow = Mathf.Clamp01(horizontalFollowFactor);
            tileWidthSourcePixels = Mathf.Max(1, sourceTileWidthPixels);
            pixelsPerUnit = Mathf.Max(1, sourcePixelsPerUnit);
            horizontalPhaseSourcePixels = horizontalPhasePixels;
            verticalScreenOffsetSourcePixels = verticalScreenOffsetPixels;
            _cameraTransform = null;
        }

        private bool TryResolveCamera()
        {
            if (_cameraTransform != null)
            {
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            _cameraTransform = mainCamera.transform;
            return true;
        }

        private static float WrapCentered(float value, float period)
        {
            if (period <= 0f)
            {
                return value;
            }

            float halfPeriod = period * 0.5f;
            return value - Mathf.Floor((value + halfPeriod) / period) * period;
        }
    }
}
