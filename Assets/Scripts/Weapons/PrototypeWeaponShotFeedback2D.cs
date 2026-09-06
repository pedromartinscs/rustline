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
        public const float TraceLength = 3f;
        public const float ImpactDuration = 0.08f;
        public const float ImpactLength = 2f / 16f;

        [SerializeField] private LineRenderer traceRenderer;
        [SerializeField] private LineRenderer impactRenderer;

        private float _traceHideTime;
        private float _impactHideTime;

        public LineRenderer TraceRenderer => traceRenderer;
        public LineRenderer ImpactRenderer => impactRenderer;
        public bool IsVisible => traceRenderer != null && traceRenderer.enabled;
        public bool IsImpactVisible => impactRenderer != null && impactRenderer.enabled;
        public Vector2 TraceStart => traceRenderer != null ? traceRenderer.GetPosition(0) : Vector2.zero;
        public Vector2 TraceEnd => traceRenderer != null ? traceRenderer.GetPosition(1) : Vector2.zero;
        public Vector2 ImpactPoint => impactRenderer != null ? impactRenderer.GetPosition(1) : Vector2.zero;

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
            if (IsVisible && Time.time >= _traceHideTime)
            {
                traceRenderer.enabled = false;
            }

            if (IsImpactVisible && Time.time >= _impactHideTime)
            {
                impactRenderer.enabled = false;
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
            float visibleLength = Mathf.Min(TraceLength, result.HitDistance);
            traceRenderer.SetPosition(0, result.EndPoint - result.Direction * visibleLength);
            traceRenderer.SetPosition(1, result.EndPoint);
            traceRenderer.enabled = true;
            _traceHideTime = Time.time + TraceDuration;

            if (impactRenderer == null || !result.Hit)
            {
                if (impactRenderer != null)
                {
                    impactRenderer.enabled = false;
                }

                return;
            }

            Vector2 normal = result.HitNormal.sqrMagnitude > 0f
                ? result.HitNormal.normalized
                : -result.Direction;
            Vector2 tangent = new Vector2(-normal.y, normal.x);
            Vector2 tip = result.EndPoint + normal * ImpactLength;
            Vector2 wing = tangent * (ImpactLength * 0.5f);
            impactRenderer.startColor = color;
            impactRenderer.endColor = color;
            impactRenderer.SetPosition(0, tip + wing);
            impactRenderer.SetPosition(1, result.EndPoint);
            impactRenderer.SetPosition(2, tip - wing);
            impactRenderer.enabled = true;
            _impactHideTime = Time.time + ImpactDuration;
        }

        public void Hide()
        {
            if (traceRenderer != null)
            {
                traceRenderer.enabled = false;
            }

            if (impactRenderer != null)
            {
                impactRenderer.enabled = false;
            }
        }
    }
}
