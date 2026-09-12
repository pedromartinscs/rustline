using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Camera-relative parallax that preserves Rustline's native-pixel presentation contract.
    /// The rendered camera is already snapped by PixelCameraFollow2D; this component runs later,
    /// follows only a configured fraction of that camera delta, then snaps its own transform to the
    /// same source-pixel grid so relative motion remains integer-pixel stable.
    /// </summary>
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class PixelSnappedParallax2D : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float horizontalFollow = 0.94f;
        [SerializeField, Range(0f, 1f)] private float verticalFollow = 0.96f;
        [SerializeField, Min(1)] private int pixelsPerUnit = 16;

        private Transform _cameraTransform;
        private Vector3 _cameraOrigin;
        private Vector3 _layerOrigin;
        private bool _anchorsCaptured;

        public float HorizontalFollow => horizontalFollow;
        public float VerticalFollow => verticalFollow;
        public int PixelsPerUnit => pixelsPerUnit;

        private void OnEnable()
        {
            _cameraTransform = null;
            _anchorsCaptured = false;
            TryCaptureAnchors();
        }

        private void LateUpdate()
        {
            if (!TryCaptureAnchors())
            {
                return;
            }

            Vector3 cameraPosition = _cameraTransform.position;
            Vector3 cameraDelta = cameraPosition - _cameraOrigin;
            Vector3 desiredPosition = new Vector3(
                _layerOrigin.x + cameraDelta.x * horizontalFollow,
                _layerOrigin.y + cameraDelta.y * verticalFollow,
                _layerOrigin.z);

            float scale = pixelsPerUnit;
            Vector3 snappedPosition = new Vector3(
                Mathf.Round(desiredPosition.x * scale) / scale,
                Mathf.Round(desiredPosition.y * scale) / scale,
                _layerOrigin.z);

            if (!transform.position.Equals(snappedPosition))
            {
                transform.position = snappedPosition;
            }
        }

        public void Configure(float horizontalFollowFactor, float verticalFollowFactor, int sourcePixelsPerUnit)
        {
            horizontalFollow = Mathf.Clamp01(horizontalFollowFactor);
            verticalFollow = Mathf.Clamp01(verticalFollowFactor);
            pixelsPerUnit = Mathf.Max(1, sourcePixelsPerUnit);
            _cameraTransform = null;
            _anchorsCaptured = false;
        }

        private bool TryCaptureAnchors()
        {
            if (_anchorsCaptured && _cameraTransform != null)
            {
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            _cameraTransform = mainCamera.transform;
            _cameraOrigin = _cameraTransform.position;
            _layerOrigin = transform.position;
            _anchorsCaptured = true;
            return true;
        }
    }
}
