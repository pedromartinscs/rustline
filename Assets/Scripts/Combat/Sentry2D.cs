using System;
using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CombatHealth2D))]
    public sealed class Sentry2D : MonoBehaviour
    {
        public const float StrikeReach = 1.8f;
        public const int StrikeDamage = 20;
        [SerializeField] private CapsuleCollider2D playerCollider;
        [SerializeField] private CombatHealth2D playerHealth;
        [SerializeField] private Collider2D weaponHitCollider;
        [SerializeField] private float minimumX;
        [SerializeField] private float maximumX;

        private Rigidbody2D _body;
        private CombatHealth2D _health;
        private Vector2 _spawn;
        private int _direction = -1;
        private int _strikeDirection;
        private float _hitPause;
        private readonly SentryBrain2D _brain = new SentryBrain2D();
        private EnemyGroundSupport2D _groundSupport;
        internal void SetVoidTopHeight(float height) => _groundSupport.SetVoidTopHeight(height);
        private Camera _worldCamera;
        internal bool RequireVisibleDetection { get; set; }
        public event Action PlayerDetected;
        public bool CombatEngaged { get; private set; } = true;
        public SentryState State => _brain.State;
        public CombatHealth2D Health => _health;
        public int StrikeCount { get; private set; }
        public Vector2 SpawnPosition => _spawn;
        public bool CombatActive { get; private set; } = true;
        public int FacingDirection { get; private set; } = -1;
        public float StateTimeRemaining => _brain.Remaining;
        public float HitPauseRemaining => _hitPause;
        // MovePosition's velocity is consumed by the physics step; retain its successful
        // movement intent until the next AI step so render frames do not alternate with idle.
        public bool IsMoving { get; private set; }

        public void SetCombatActive(bool active)
        {
            CombatActive = active;
            if (!active) IsMoving = false;
            if (!active && _body != null) _body.linearVelocity = Vector2.zero;
        }

        public void SetCombatEngaged(bool engaged)
        {
            CombatEngaged = engaged;
            if (!engaged && _health != null && _health.IsAlive) _brain.Reset();
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<CombatHealth2D>();
            _spawn = _body.position;
            _groundSupport = new EnemyGroundSupport2D(_body, weaponHitCollider);
        }

        private void OnEnable()
        {
            if (_health == null) _health = GetComponent<CombatHealth2D>();
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        public static bool InDetectionWindow(Vector2 origin, Vector2 target) =>
            Mathf.Abs(origin.x - target.x) <= 10f && Mathf.Abs(origin.y - target.y) <= 3f;

        public static bool InStrikeWindow(Vector2 origin, Vector2 target, int facing) =>
            Mathf.Abs(target.x - origin.x) <= StrikeReach &&
            Mathf.Abs(target.y - origin.y) <= 1.5f &&
            (target.x - origin.x) * facing >= -0.15f;

        private void FixedUpdate()
        {
            IsMoving = false;
            if (!CombatActive && !_health.IsDead) return;
            if (_groundSupport.Step(_body, _health, Time.fixedDeltaTime)) return;
            if (_health.IsDead) return;
            if (_hitPause > 0f) { _hitPause -= Time.fixedDeltaTime; return; }
            Vector2 sensor = _body.position + Vector2.up * 1.25f;
            Vector2 target = playerCollider != null ? (Vector2)playerCollider.bounds.center : sensor;
            Vector2 delta = target - sensor;
            bool sees = CanDetectPlayer();
            if (sees && !CombatEngaged) PlayerDetected?.Invoke();
            sees &= CombatEngaged;
            bool reach = sees && Mathf.Abs(delta.x) <= StrikeReach && Mathf.Abs(delta.y) <= 1.5f;
            if (sees && State != SentryState.Windup && State != SentryState.Recovery)
                FacingDirection = delta.x < 0f ? -1 : 1;
            SentryState previous = State;
            bool strike = _brain.Step(Time.fixedDeltaTime, sees, reach);
            if (State == SentryState.Windup && previous != State)
            {
                _strikeDirection = delta.x < 0f ? -1 : 1;
                FacingDirection = _strikeDirection;
            }
            if (strike)
            {
                StrikeCount++;
                if (sees && InStrikeWindow(sensor, target, _strikeDirection))
                {
                    var damage = new DamageInfo2D(StrikeDamage, target, Vector2.right * _strikeDirection, this);
                    playerHealth.ApplyDamage(in damage);
                }
            }
            if (State == SentryState.Patrol) Move(_direction, 1.3f);
            else if (State == SentryState.Pursuit && !reach) Move(delta.x < 0f ? -1 : 1, 2.8f);
        }

        public bool CanDetectPlayer()
        {
            if (!CombatActive || _health == null || _health.IsDead || playerHealth == null ||
                !playerHealth.IsAlive || playerCollider == null || !playerCollider.enabled) return false;
            Vector2 sensor = _body.position + Vector2.up * 1.25f;
            Vector2 target = playerCollider.bounds.center;
            if (!InDetectionWindow(sensor, target) ||
                RequireVisibleDetection && !EnemyPerception2D.InView(ref _worldCamera, sensor)) return false;
            Vector2 delta = target - sensor;
            return Physics2D.Raycast(sensor, delta.normalized, delta.magnitude, 1 << 6).collider == null;
        }

        private void Move(int direction, float speed)
        {
            float next = _body.position.x + direction * speed * Time.fixedDeltaTime;
            Vector2 footAhead = new Vector2(next + direction * 0.65f, _body.position.y + 0.3f);
            bool supported = Physics2D.Raycast(footAhead, Vector2.down, 0.7f, 1 << 6).collider != null;
            bool blocked = _groundSupport.BlocksHorizontal(_body, direction);
            if (next < minimumX || next > maximumX || !supported || blocked)
            {
                _direction = -direction;
                FacingDirection = _direction;
                return;
            }
            _direction = direction;
            FacingDirection = direction;
            _body.MovePosition(new Vector2(next, _body.position.y));
            IsMoving = true;
        }

        public void ResetForEncounter()
        {
            _groundSupport.Reset();
            _brain.Reset();
            _hitPause = 0f;
            _direction = -1;
            FacingDirection = -1;
            IsMoving = false;
            transform.position = _spawn;
            _body.position = _spawn;
            _body.linearVelocity = Vector2.zero;
            _health.ResetHealth();
            weaponHitCollider.enabled = true;
        }

        private void OnDamaged(DamageResult2D result) { if (result.DidApply) _hitPause = 0.12f; }
        private void OnDied(DamageResult2D result)
        {
            _brain.Kill();
            IsMoving = false;
            _body.linearVelocity = Vector2.zero;
            weaponHitCollider.enabled = false;
            _hitPause = 0f;
        }
    }
}
