using System;
using System.Collections.Generic;
using Rustline.Presentation;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    public enum BombardierState { Patrol, Windup, Recover, Dead }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CombatHealth2D))]
    public sealed class Bombardier2D : MonoBehaviour
    {
        public const float DefaultPatrolSpeed = 1.75f;
        public const float DefaultPatrolHalfDistance = 2f;
        public const float DefaultHitPauseDuration = 0.12f;
        public const float DetectionHorizontalRange = 12f;
        public const float DetectionVerticalRange = 6f;
        public const float WindupDuration = 0.70f;
        public const float RecoveryDuration = 1.30f;

        [SerializeField] private CombatHealth2D health;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D weaponHitCollider;
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private BombardierPresenter2D presenter;
        [SerializeField, Min(0f)] private float patrolSpeed = DefaultPatrolSpeed;
        [SerializeField, Min(0f)] private float patrolHalfDistance = DefaultPatrolHalfDistance;
        [SerializeField] private int initialDirection = 1;
        [SerializeField, Min(0f)] private float hitPauseDuration = DefaultHitPauseDuration;

        private readonly List<BombardierBomb2D> _bombs = new List<BombardierBomb2D>();
        private CombatHealth2D _playerHealth;
        private CapsuleCollider2D _playerCollider;
        private Vector2 _spawnPosition;
        private int _patrolDirection;
        private float _hitPauseRemaining;
        private float _stateTimeRemaining;

        public event Action Died;
        public CombatHealth2D Health => health;
        public Rigidbody2D Body => body;
        public Collider2D WeaponHitCollider => weaponHitCollider;
        public Transform ThrowOrigin => throwOrigin;
        public Vector2 SpawnPosition => _spawnPosition;
        public float PatrolSpeed => patrolSpeed;
        public float PatrolHalfDistance => patrolHalfDistance;
        public float PatrolMinimumX => _spawnPosition.x - patrolHalfDistance;
        public float PatrolMaximumX => _spawnPosition.x + patrolHalfDistance;
        public int InitialDirection => initialDirection >= 0 ? 1 : -1;
        public int PatrolDirection => _patrolDirection;
        public float HitPauseRemaining => _hitPauseRemaining;
        public float StateTimeRemaining => _stateTimeRemaining;
        public BombardierState State { get; private set; } = BombardierState.Patrol;
        public Vector2 LockedTargetPoint { get; private set; }
        public int LaunchCount { get; private set; }
        public int ActiveBombCount => _bombs.Count;
        public bool IsPatrolling => State == BombardierState.Patrol && _hitPauseRemaining <= 0f;

        private void Awake()
        {
            if (health == null) health = GetComponent<CombatHealth2D>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (presenter == null) presenter = GetComponentInChildren<BombardierPresenter2D>();
            PlayerMotor2D player = FindAnyObjectByType<PlayerMotor2D>();
            if (player != null)
            {
                _playerHealth = player.GetComponent<CombatHealth2D>();
                _playerCollider = player.GetComponent<CapsuleCollider2D>();
            }
            _spawnPosition = body != null ? body.position : (Vector2)transform.position;
            _patrolDirection = InitialDirection;
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void FixedUpdate()
        {
            if (body == null || health == null || State == BombardierState.Dead) return;
            float dt = Time.fixedDeltaTime;
            if (_hitPauseRemaining > 0f)
            {
                _hitPauseRemaining = Mathf.Max(0f, _hitPauseRemaining - dt);
                return;
            }

            switch (State)
            {
                case BombardierState.Patrol:
                    if (CanDetectPlayer()) BeginWindup();
                    else AdvancePatrol(dt);
                    break;
                case BombardierState.Windup:
                    _stateTimeRemaining -= dt;
                    if (_stateTimeRemaining <= 0f)
                    {
                        LaunchBomb();
                        State = BombardierState.Recover;
                        _stateTimeRemaining = RecoveryDuration;
                    }
                    break;
                case BombardierState.Recover:
                    _stateTimeRemaining -= dt;
                    if (_stateTimeRemaining <= 0f)
                    {
                        if (CanDetectPlayer()) BeginWindup();
                        else State = BombardierState.Patrol;
                    }
                    break;
            }
        }

        public static bool IsInsideDetectionRange(Vector2 sensor, Vector2 target) =>
            Mathf.Abs(target.x - sensor.x) <= DetectionHorizontalRange &&
            Mathf.Abs(target.y - sensor.y) <= DetectionVerticalRange;

        public bool CanDetectPlayer()
        {
            if (_playerHealth == null || !_playerHealth.IsAlive ||
                _playerCollider == null || !_playerCollider.enabled) return false;
            Vector2 sensor = body.position + new Vector2(0f, 1.25f);
            Vector2 target = _playerCollider.bounds.center;
            if (!IsInsideDetectionRange(sensor, target)) return false;
            Vector2 delta = target - sensor;
            return Physics2D.Raycast(sensor, delta.normalized, delta.magnitude, 1 << 6).collider == null;
        }

        public void ResetForEncounter()
        {
            ClearActiveBombs();
            State = BombardierState.Patrol;
            _stateTimeRemaining = 0f;
            _hitPauseRemaining = 0f;
            _patrolDirection = InitialDirection;
            LockedTargetPoint = default;
            if (body != null)
            {
                body.position = _spawnPosition;
                body.rotation = 0f;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            if (weaponHitCollider != null) weaponHitCollider.enabled = true;
            health?.ResetHealth();
            Physics2D.SyncTransforms();
        }

        public void ClearActiveBombs()
        {
            for (int index = _bombs.Count - 1; index >= 0; index--)
            {
                if (_bombs[index] != null) _bombs[index].CancelForReset();
            }
            _bombs.Clear();
        }

        internal void UnregisterBomb(BombardierBomb2D bomb) => _bombs.Remove(bomb);

        private void AdvancePatrol(float dt)
        {
            float nextX = body.position.x + _patrolDirection * patrolSpeed * dt;
            if (nextX >= PatrolMaximumX) { nextX = PatrolMaximumX; _patrolDirection = -1; }
            else if (nextX <= PatrolMinimumX) { nextX = PatrolMinimumX; _patrolDirection = 1; }
            body.MovePosition(new Vector2(nextX, _spawnPosition.y));
        }

        private void BeginWindup()
        {
            State = BombardierState.Windup;
            _stateTimeRemaining = WindupDuration;
            LockedTargetPoint = _playerCollider.bounds.center;
            _patrolDirection = LockedTargetPoint.x < body.position.x ? -1 : 1;
        }

        private void LaunchBomb()
        {
            Vector2 origin = throwOrigin != null ? throwOrigin.position : body.position + Vector2.up * 2f;
            GameObject bombObject = new GameObject("Bombardier Bomb");
            bombObject.transform.position = origin;
            BombardierBomb2D bomb = bombObject.AddComponent<BombardierBomb2D>();
            Material material = presenter != null && presenter.SilhouetteRenderer != null
                ? presenter.SilhouetteRenderer.sharedMaterial : null;
            bomb.Launch(this, origin, LockedTargetPoint, _playerHealth, _playerCollider, material);
            _bombs.Add(bomb);
            LaunchCount++;
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (result.DidApply && health.IsAlive) _hitPauseRemaining = hitPauseDuration;
        }

        private void OnDied(DamageResult2D result)
        {
            State = BombardierState.Dead;
            _stateTimeRemaining = 0f;
            _hitPauseRemaining = 0f;
            if (body != null) { body.linearVelocity = Vector2.zero; body.angularVelocity = 0f; }
            if (weaponHitCollider != null) weaponHitCollider.enabled = false;
            Died?.Invoke();
        }
    }
}
