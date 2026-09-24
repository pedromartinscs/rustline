using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BombardierPresenter2D : MonoBehaviour
    {
        public const float HitReactionDuration = 0.12f;
        [SerializeField] private CombatHealth2D health;
        [SerializeField] private Bombardier2D enemy;
        [SerializeField] private LineRenderer silhouetteRenderer;

        private LineRenderer _targetMarker;
        private float _hitReactionRemaining;
        public CombatHealth2D Health => health;
        public LineRenderer SilhouetteRenderer => silhouetteRenderer;
        public bool IsHitReacting => _hitReactionRemaining > 0f;
        public bool IsDeadVisual => silhouetteRenderer != null && !silhouetteRenderer.enabled;
        public int HitReactionCount { get; private set; }
        public int DeathPresentationCount { get; private set; }
        public int ResetPresentationCount { get; private set; }

        private void Awake()
        {
            if (silhouetteRenderer == null) silhouetteRenderer = GetComponent<LineRenderer>();
            if (enemy == null) enemy = GetComponentInParent<Bombardier2D>();
            GameObject marker = new GameObject("Locked Target Marker");
            _targetMarker = marker.AddComponent<LineRenderer>();
            _targetMarker.sharedMaterial = silhouetteRenderer.sharedMaterial;
            _targetMarker.useWorldSpace = false;
            _targetMarker.loop = true;
            _targetMarker.positionCount = 4;
            _targetMarker.widthMultiplier = 0.0625f;
            _targetMarker.sortingOrder = 31;
            Color color = RustlinePalette.GetColor(13);
            _targetMarker.startColor = color;
            _targetMarker.endColor = color;
            _targetMarker.SetPosition(0, new Vector3(-0.25f, 0f));
            _targetMarker.SetPosition(1, new Vector3(0f, 0.25f));
            _targetMarker.SetPosition(2, new Vector3(0.25f, 0f));
            _targetMarker.SetPosition(3, new Vector3(0f, -0.25f));
            _targetMarker.enabled = false;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
                health.HealthReset += OnHealthReset;
            }
            ApplyHealthState();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
                health.HealthReset -= OnHealthReset;
            }
            if (_targetMarker != null) _targetMarker.enabled = false;
        }

        private void OnDestroy()
        {
            if (_targetMarker != null) Destroy(_targetMarker.gameObject);
        }

        private void Update()
        {
            if (_hitReactionRemaining > 0f)
                _hitReactionRemaining = Mathf.Max(0f, _hitReactionRemaining - Time.deltaTime);
            if (enemy != null && _targetMarker != null)
            {
                _targetMarker.enabled = enemy.State == BombardierState.Windup;
                if (_targetMarker.enabled) _targetMarker.transform.position = enemy.LockedTargetPoint;
            }
            if (health != null && health.IsAlive)
            {
                int colorIndex = _hitReactionRemaining > 0f ? 24 :
                    enemy != null && enemy.State == BombardierState.Windup ? 13 : 19;
                ApplyColor(RustlinePalette.GetColor(colorIndex));
            }
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (!result.DidApply || health == null || health.IsDead) return;
            HitReactionCount++;
            _hitReactionRemaining = HitReactionDuration;
            ApplyColor(RustlinePalette.GetColor(24));
        }

        private void OnDied(DamageResult2D result)
        {
            _hitReactionRemaining = 0f;
            DeathPresentationCount++;
            if (silhouetteRenderer != null) silhouetteRenderer.enabled = false;
            if (_targetMarker != null) _targetMarker.enabled = false;
        }

        private void OnHealthReset()
        {
            _hitReactionRemaining = 0f;
            ResetPresentationCount++;
            if (silhouetteRenderer != null) silhouetteRenderer.enabled = true;
            if (_targetMarker != null) _targetMarker.enabled = false;
            ApplyColor(RustlinePalette.GetColor(19));
        }

        private void ApplyHealthState()
        {
            if (silhouetteRenderer == null) return;
            silhouetteRenderer.enabled = health == null || health.IsAlive;
            if (silhouetteRenderer.enabled) ApplyColor(RustlinePalette.GetColor(19));
        }

        private void ApplyColor(Color color)
        {
            if (silhouetteRenderer == null) return;
            silhouetteRenderer.startColor = color;
            silhouetteRenderer.endColor = color;
        }
    }
}
