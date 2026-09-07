using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PrototypeGroundEnemyPresenter2D : MonoBehaviour
    {
        public const float HitReactionDuration = 0.12f;

        [SerializeField] private CombatHealth2D health;
        [SerializeField] private LineRenderer silhouetteRenderer;

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
            if (silhouetteRenderer == null)
            {
                silhouetteRenderer = GetComponent<LineRenderer>();
            }
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
        }

        private void Update()
        {
            if (_hitReactionRemaining <= 0f)
            {
                return;
            }

            _hitReactionRemaining = Mathf.Max(0f, _hitReactionRemaining - Time.deltaTime);
            if (_hitReactionRemaining <= 0f && health != null && health.IsAlive)
            {
                ApplyColor(RustlinePalette.GetColor(19));
            }
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (!result.DidApply || health == null || health.IsDead)
            {
                return;
            }

            HitReactionCount++;
            _hitReactionRemaining = HitReactionDuration;
            ApplyColor(RustlinePalette.GetColor(24));
        }

        private void OnDied(DamageResult2D result)
        {
            _hitReactionRemaining = 0f;
            DeathPresentationCount++;
            if (silhouetteRenderer != null)
            {
                silhouetteRenderer.enabled = false;
            }
        }

        private void OnHealthReset()
        {
            _hitReactionRemaining = 0f;
            ResetPresentationCount++;
            if (silhouetteRenderer != null)
            {
                silhouetteRenderer.enabled = true;
            }

            ApplyColor(RustlinePalette.GetColor(19));
        }

        private void ApplyHealthState()
        {
            if (silhouetteRenderer == null)
            {
                return;
            }

            bool alive = health == null || health.IsAlive;
            silhouetteRenderer.enabled = alive;
            if (alive)
            {
                ApplyColor(RustlinePalette.GetColor(19));
            }
        }

        private void ApplyColor(Color color)
        {
            if (silhouetteRenderer == null)
            {
                return;
            }

            silhouetteRenderer.startColor = color;
            silhouetteRenderer.endColor = color;
        }
    }
}
