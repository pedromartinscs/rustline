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

        private void OnEnable()
        {
            _continuousPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 destination = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);

            _continuousPosition = Vector3.SmoothDamp(
                _continuousPosition,
                destination,
                ref _smoothVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            float scale = pixelsPerUnit;
            transform.position = new Vector3(
                Mathf.Round(_continuousPosition.x * scale) / scale,
                Mathf.Round(_continuousPosition.y * scale) / scale,
                _continuousPosition.z);
        }
    }
}
