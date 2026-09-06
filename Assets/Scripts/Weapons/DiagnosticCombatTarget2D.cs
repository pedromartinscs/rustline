using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    /// <summary>
    /// Repeatable MovementLab-only receiver; this is not the production health/death model.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(LineRenderer))]
    public sealed class DiagnosticCombatTarget2D : MonoBehaviour, IWeaponHitReceiver2D
    {
        public const float HitFlashDuration = 0.12f;

        [SerializeField] private LineRenderer targetRenderer;

        private float _flashTimeRemaining;

        public int HitsTaken { get; private set; }
        public int AccumulatedDamage { get; private set; }
        public Vector2 LastHitPoint { get; private set; }
        public Vector2 LastHitDirection { get; private set; }
        public LineRenderer TargetRenderer => targetRenderer;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<LineRenderer>();
            }
        }

        private void OnEnable()
        {
            ApplyColor(RustlinePalette.GetColor(14));
        }

        private void Update()
        {
            if (_flashTimeRemaining <= 0f)
            {
                return;
            }

            _flashTimeRemaining = Mathf.Max(0f, _flashTimeRemaining - Time.deltaTime);
            if (_flashTimeRemaining <= 0f)
            {
                ApplyColor(RustlinePalette.GetColor(14));
            }
        }

        public void ReceiveHit(in WeaponHitInfo2D hit)
        {
            HitsTaken++;
            AccumulatedDamage += hit.Damage;
            LastHitPoint = hit.Point;
            LastHitDirection = hit.Direction;
            _flashTimeRemaining = HitFlashDuration;
            ApplyColor(RustlinePalette.GetColor(24));
        }

        public void ResetDiagnostics()
        {
            HitsTaken = 0;
            AccumulatedDamage = 0;
            LastHitPoint = Vector2.zero;
            LastHitDirection = Vector2.zero;
            _flashTimeRemaining = 0f;
            ApplyColor(RustlinePalette.GetColor(14));
        }

        private void ApplyColor(Color color)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.startColor = color;
            targetRenderer.endColor = color;
        }
    }
}
