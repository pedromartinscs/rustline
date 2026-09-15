using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    /// <summary>
    /// Reused programmer-art distal trace for hitscan weapons.
    /// Impact-line feedback is intentionally disabled until authored collision particles land.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PrototypeWeaponShotFeedback2D : MonoBehaviour
    {
        public const float TraceDuration = 0.06f;
        public const float TraceWidth = 1f / 16f;
        public const float TraceLength = 3f;

        [SerializeField] private LineRenderer traceRenderer;
        [SerializeField] private LineRenderer impactRenderer;

        private float _traceHideTime;
        private bool _traceActive;

        public LineRenderer TraceRenderer => traceRenderer;
        public LineRenderer ImpactRenderer => impactRenderer;
        public bool IsVisible => traceRenderer != null && traceRenderer.enabled;
        public bool IsImpactVisible => false;
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
            if (!_traceActive)
            {
                return;
            }

            if (Time.time >= _traceHideTime)
            {
                _traceActive = false;
                if (traceRenderer != null)
                {
                    traceRenderer.enabled = false;
                }
            }
        }

        public void Show(in WeaponShotResult2D result)
        {
            if (impactRenderer != null)
            {
                impactRenderer.enabled = false;
            }

            if (traceRenderer == null)
            {
                return;
            }

            Color color = result.HitReceiverNotified
                ? RustlinePalette.GetColor(24)
                : result.Hit
                    ? RustlinePalette.GetColor(12)
                    : result.ShotMode == WeaponShotMode2D.Bouncing
                        ? RustlinePalette.GetColor(22)
                        : RustlinePalette.GetColor(20);
            traceRenderer.startColor = color;
            traceRenderer.endColor = color;
            Vector2 finalDirection = result.FinalDirection.sqrMagnitude > 0f
                ? result.FinalDirection.normalized
                : result.Direction.normalized;
            float visibleLength = Mathf.Min(TraceLength, result.HitDistance);
            traceRenderer.SetPosition(0, result.EndPoint - finalDirection * visibleLength);
            traceRenderer.SetPosition(1, result.EndPoint);
            traceRenderer.enabled = true;
            _traceActive = true;
            _traceHideTime = Time.time + TraceDuration;
        }

        public void Hide()
        {
            _traceActive = false;
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
