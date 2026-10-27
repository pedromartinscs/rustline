using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Two-axis version of the existing wrapped, source-pixel-snapped parallax model.</summary>
    [DefaultExecutionOrder(270)]
    [DisallowMultipleComponent]
    public sealed class PixelSnappedBackgroundPlane2D : MonoBehaviour
    {
        [SerializeField] private Vector2 cameraFollow = new Vector2(0.94f, 0.96f);
        [SerializeField] private Vector2 repeatWorldSize = new Vector2(32f, 32f);
        private Transform _camera;
        private float _z;
        public Vector2 CameraFollow => cameraFollow;
        public Vector2 RepeatWorldSize => repeatWorldSize;

        public void Configure(Vector2 follow, Vector2 repeat)
        {
            cameraFollow = new Vector2(Mathf.Clamp01(follow.x), Mathf.Clamp01(follow.y));
            repeatWorldSize = new Vector2(Mathf.Max(1f,repeat.x), Mathf.Max(1f,repeat.y));
        }

        private void OnEnable() { _camera = null; _z = transform.position.z; }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                Camera world = Camera.main;
                if (world == null) return;
                _camera = world.transform;
            }
            Vector3 position = PositionForCamera(_camera.position, cameraFollow, repeatWorldSize);
            position.z = _z;
            if (transform.position != position) transform.position = position;
        }

        public static Vector3 PositionForCamera(Vector3 camera, Vector2 follow, Vector2 repeat)
        {
            float x = camera.x + WrapCentered(camera.x * (follow.x - 1f), repeat.x);
            float y = camera.y + WrapCentered(camera.y * (follow.y - 1f), repeat.y);
            return new Vector3(Mathf.Round(x*16f)/16f, Mathf.Round(y*16f)/16f, 0f);
        }

        private static float WrapCentered(float value, float period) =>
            value - Mathf.Floor((value + period*0.5f)/period)*period;
    }
}
