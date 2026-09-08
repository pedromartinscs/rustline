using UnityEngine;

namespace Rustline.Presentation
{
    public sealed class PixelCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 2f);
        [SerializeField, Min(0.001f)] private float smoothTime = 0.08f;
        [SerializeField, Min(1)] private int pixelsPerUnit = 16;

        private Vector3 _continuousPosition;
        private Vector3 _smoothVelocity;
        private Vector2 _presentationOffset;
        private Transform _cameraTransform;

        public Vector3 ContinuousFollowPosition => _continuousPosition;
        public Vector2 PresentationOffset => _presentationOffset;

        private void OnEnable()
        {
            _cameraTransform = transform;
            _continuousPosition = _cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position;
            Vector3 cameraPosition = _cameraTransform.position;
            Vector3 destination = new Vector3(
                targetPosition.x + offset.x,
                targetPosition.y + offset.y,
                cameraPosition.z);

            _continuousPosition = Vector3.SmoothDamp(
                _continuousPosition,
                destination,
                ref _smoothVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            float scale = pixelsPerUnit;
            Vector3 snappedPosition = new Vector3(
                Mathf.Round((_continuousPosition.x + _presentationOffset.x) * scale) / scale,
                Mathf.Round((_continuousPosition.y + _presentationOffset.y) * scale) / scale,
                _continuousPosition.z);

            // Continue smoothing below pixel resolution, but do not dirty the camera
            // Transform until its exact rendered position changes (including recoil).
            if (!snappedPosition.Equals(cameraPosition))
            {
                _cameraTransform.position = snappedPosition;
            }
        }

        /// <summary>
        /// Adds presentation-only camera feedback before final native-pixel snapping.
        /// The followed position remains independent, so clearing the offset cannot drift it.
        /// </summary>
        public void SetPresentationOffset(Vector2 value)
        {
            _presentationOffset = value;
        }
    }
}
