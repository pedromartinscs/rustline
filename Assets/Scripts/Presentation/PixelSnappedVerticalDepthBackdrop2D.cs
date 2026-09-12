using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Pixel-snapped vertical depth backdrop. The layer follows camera X exactly while its Y offset
    /// reveals progressively higher portions of a non-repeating source image as camera altitude rises.
    /// The complete phase height drives the mapping, with a configurable minimum virtual span so short
    /// phases intentionally reveal only part of the backdrop.
    /// </summary>
    [DefaultExecutionOrder(260)]
    [DisallowMultipleComponent]
    public sealed class PixelSnappedVerticalDepthBackdrop2D : MonoBehaviour
    {
        [SerializeField] private float phaseBottomWorldY;
        [SerializeField] private float phaseTopWorldY = 19f;
        [SerializeField, Min(1)] private int sourceHeightPixels = 2160;
        [SerializeField, Min(1)] private int minimumPhaseHeightPixels = 4320;
        [SerializeField, Min(1)] private int pixelsPerUnit = 16;

        private Camera _camera;
        private Transform _cameraTransform;
        private float _layerZ;

        public float PhaseBottomWorldY => phaseBottomWorldY;
        public float PhaseTopWorldY => phaseTopWorldY;
        public int SourceHeightPixels => sourceHeightPixels;
        public int MinimumPhaseHeightPixels => minimumPhaseHeightPixels;
        public int PixelsPerUnit => pixelsPerUnit;
        public float ActualPhaseHeightWorldUnits => Mathf.Max(0f, phaseTopWorldY - phaseBottomWorldY);
        public float MinimumPhaseHeightWorldUnits => minimumPhaseHeightPixels / (float)pixelsPerUnit;
        public float EffectivePhaseHeightWorldUnits =>
            Mathf.Max(ActualPhaseHeightWorldUnits, MinimumPhaseHeightWorldUnits);

        private void OnEnable()
        {
            _camera = null;
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
            float sourceHeightWorld = sourceHeightPixels / scale;
            float viewportHeightWorld = _camera.orthographicSize * 2f;
            float revealTravelWorld = Mathf.Max(0f, sourceHeightWorld - viewportHeightWorld);

            float altitudeAboveBottom = Mathf.Max(0f, cameraPosition.y - phaseBottomWorldY);
            float altitudeT = Mathf.Clamp01(altitudeAboveBottom / EffectivePhaseHeightWorldUnits);

            // At the phase bottom the sprite is shifted upward so the camera sees the lowest portion
            // of the source. As altitude increases the offset moves toward the opposite extreme,
            // progressively revealing higher source rows without ever repeating vertically.
            float verticalOffset = Mathf.Lerp(
                revealTravelWorld * 0.5f,
                -revealTravelWorld * 0.5f,
                altitudeT);

            Vector3 desiredPosition = new Vector3(
                cameraPosition.x,
                cameraPosition.y + verticalOffset,
                _layerZ);

            Vector3 snappedPosition = new Vector3(
                Mathf.Round(desiredPosition.x * scale) / scale,
                Mathf.Round(desiredPosition.y * scale) / scale,
                _layerZ);

            if (!transform.position.Equals(snappedPosition))
            {
                transform.position = snappedPosition;
            }
        }

        public void Configure(
            float phaseBottomY,
            float phaseTopY,
            int sourceBackdropHeightPixels,
            int minimumVirtualPhaseHeightPixels,
            int sourcePixelsPerUnit)
        {
            phaseBottomWorldY = phaseBottomY;
            phaseTopWorldY = Mathf.Max(phaseBottomY, phaseTopY);
            sourceHeightPixels = Mathf.Max(1, sourceBackdropHeightPixels);
            minimumPhaseHeightPixels = Mathf.Max(1, minimumVirtualPhaseHeightPixels);
            pixelsPerUnit = Mathf.Max(1, sourcePixelsPerUnit);
            _camera = null;
            _cameraTransform = null;
        }

        private bool TryResolveCamera()
        {
            if (_camera != null && _cameraTransform != null)
            {
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            _camera = mainCamera;
            _cameraTransform = mainCamera.transform;
            return true;
        }
    }
}
