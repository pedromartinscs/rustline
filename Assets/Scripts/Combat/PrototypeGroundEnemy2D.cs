using System;
using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CombatHealth2D))]
    public sealed class PrototypeGroundEnemy2D : MonoBehaviour
    {
        public const float DefaultPatrolSpeed = 1.75f;
        public const float DefaultPatrolHalfDistance = 2f;
        public const float DefaultHitPauseDuration = 0.12f;

        [SerializeField] private CombatHealth2D health;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D weaponHitCollider;
        [SerializeField, Min(0f)] private float patrolSpeed = DefaultPatrolSpeed;
        [SerializeField, Min(0f)] private float patrolHalfDistance = DefaultPatrolHalfDistance;
        [SerializeField] private int initialDirection = 1;
        [SerializeField, Min(0f)] private float hitPauseDuration = DefaultHitPauseDuration;

        private Vector2 _spawnPosition;
        private int _patrolDirection;
        private float _hitPauseRemaining;

        public event Action Died;

        public CombatHealth2D Health => health;
        public Rigidbody2D Body => body;
        public Collider2D WeaponHitCollider => weaponHitCollider;
        public Vector2 SpawnPosition => _spawnPosition;
        public float PatrolSpeed => patrolSpeed;
        public float PatrolHalfDistance => patrolHalfDistance;
        public float PatrolMinimumX => _spawnPosition.x - patrolHalfDistance;
        public float PatrolMaximumX => _spawnPosition.x + patrolHalfDistance;
        public int InitialDirection => initialDirection >= 0 ? 1 : -1;
        public int PatrolDirection => _patrolDirection;
        public float HitPauseRemaining => _hitPauseRemaining;
        public bool IsPatrolling => health != null && health.IsAlive && _hitPauseRemaining <= 0f;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<CombatHealth2D>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            _spawnPosition = body != null ? body.position : (Vector2)transform.position;
            _patrolDirection = InitialDirection;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void FixedUpdate()
        {
            if (health == null || body == null || health.IsDead)
            {
                return;
            }

            if (_hitPauseRemaining > 0f)
            {
                _hitPauseRemaining = Mathf.Max(0f, _hitPauseRemaining - Time.fixedDeltaTime);
                return;
            }

            float nextX = body.position.x + _patrolDirection * patrolSpeed * Time.fixedDeltaTime;
            if (nextX >= PatrolMaximumX)
            {
                nextX = PatrolMaximumX;
                _patrolDirection = -1;
            }
            else if (nextX <= PatrolMinimumX)
            {
                nextX = PatrolMinimumX;
                _patrolDirection = 1;
            }

            body.MovePosition(new Vector2(nextX, _spawnPosition.y));
        }

        public void ResetForEncounter()
        {
            _patrolDirection = InitialDirection;
            _hitPauseRemaining = 0f;
            if (body != null)
            {
                body.position = _spawnPosition;
                body.rotation = 0f;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            else
            {
                transform.position = _spawnPosition;
            }

            if (weaponHitCollider != null)
            {
                weaponHitCollider.enabled = true;
            }

            health?.ResetHealth();
            Physics2D.SyncTransforms();
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (result.DidApply && health != null && health.IsAlive)
            {
                _hitPauseRemaining = hitPauseDuration;
            }
        }

        private void OnDied(DamageResult2D result)
        {
            _hitPauseRemaining = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            if (weaponHitCollider != null)
            {
                weaponHitCollider.enabled = false;
            }

            Died?.Invoke();
        }
    }
}
