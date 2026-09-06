using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    /// <summary>
    /// Reused programmer-art trace. Exact authored muzzle metadata is intentionally deferred.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PrototypeWeaponShotFeedback2D : MonoBehaviour
    {
        public const float TraceDuration = 0.06f;
        public const float TraceWidth = 1f / 16f;

        [SerializeField] private LineRenderer traceRenderer;

        private float _hideTime;

        public LineRenderer TraceRenderer => traceRenderer;
        public bool IsVisible => traceRenderer != null && traceRenderer.enabled;

        private void Awake()
        {
            if (traceRenderer == null)
            {
                traceRenderer = GetComponent<LineRenderer>();
            }

            Hide();
        }

        private void Update()
        {
            if (IsVisible && Time.time >= _hideTime)
            {
                Hide();
            }
        }

        public void Show(in WeaponShotResult2D result)
        {
            if (traceRenderer == null)
            {
                return;
            }

            Color color = result.HitReceiverNotified
                ? RustlinePalette.GetColor(24)
                : result.Hit
                    ? RustlinePalette.GetColor(12)
                    : RustlinePalette.GetColor(20);
            traceRenderer.startColor = color;
            traceRenderer.endColor = color;
            traceRenderer.SetPosition(0, result.Origin);
            traceRenderer.SetPosition(1, result.EndPoint);
            traceRenderer.enabled = true;
            _hideTime = Time.time + TraceDuration;
        }

        public void Hide()
        {
            if (traceRenderer != null)
            {
                traceRenderer.enabled = false;
            }
        }
    }
}
