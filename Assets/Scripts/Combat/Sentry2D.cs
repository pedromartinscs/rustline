using Rustline.Presentation;
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
        [SerializeField] private LineRenderer silhouette;
        [SerializeField] private float minimumX;
        [SerializeField] private float maximumX;

        private Rigidbody2D _body;
        private CombatHealth2D _health;
        private Vector2 _spawn;
        private int _direction = -1;
        private int _strikeDirection;
        private float _hitPause;
        private float _deadTime;
        private readonly SentryBrain2D _brain = new SentryBrain2D();
        public SentryState State => _brain.State;
        public CombatHealth2D Health => _health;
        public int StrikeCount { get; private set; }
        public Vector2 SpawnPosition => _spawn;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<CombatHealth2D>();
            _spawn = _body.position;
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
            if (_health.IsDead) return;
            if (_hitPause > 0f) { _hitPause -= Time.fixedDeltaTime; return; }
            Vector2 sensor = _body.position + Vector2.up * 1.25f;
            Vector2 target = playerCollider != null ? (Vector2)playerCollider.bounds.center : sensor;
            Vector2 delta = target - sensor;
            bool clear = Physics2D.Raycast(sensor, delta.normalized, delta.magnitude, 1 << 6).collider == null;
            bool sees = playerHealth != null && playerHealth.IsAlive && playerCollider != null && playerCollider.enabled &&
                        InDetectionWindow(sensor, target) && clear;
            bool reach = sees && Mathf.Abs(delta.x) <= StrikeReach && Mathf.Abs(delta.y) <= 1.5f;
            SentryState previous = State;
            bool strike = _brain.Step(Time.fixedDeltaTime, sees, reach);
            if (State == SentryState.Windup && previous != State)
                _strikeDirection = delta.x < 0f ? -1 : 1;
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

        private void Move(int direction, float speed)
        {
            float next = _body.position.x + direction * speed * Time.fixedDeltaTime;
            Vector2 footAhead = new Vector2(next + direction * 0.65f, _spawn.y + 0.3f);
            bool supported = Physics2D.Raycast(footAhead, Vector2.down, 0.7f, 1 << 6).collider != null;
            bool blocked = Physics2D.Raycast(_body.position + Vector2.up, Vector2.right * direction,
                0.8f, 1 << 6).collider != null;
            if (next < minimumX || next > maximumX || !supported || blocked)
            {
                _direction = -direction;
                return;
            }
            _direction = direction;
            _body.MovePosition(new Vector2(next, _spawn.y));
        }

        private void Update()
        {
            if (silhouette == null) return;
            if (State == SentryState.Dead)
            {
                _deadTime += Time.deltaTime;
                silhouette.transform.localScale = new Vector3(1f, Mathf.Max(0.1f, 1f - _deadTime * 4f), 1f);
                silhouette.enabled = _deadTime < 0.4f;
            }
            int color = _hitPause > 0f ? 24 : State == SentryState.Windup ? 13 :
                State == SentryState.Alert || State == SentryState.Pursuit ? 12 : 10;
            silhouette.startColor = silhouette.endColor = RustlinePalette.GetColor(color);
        }

        public void ResetForEncounter()
        {
            _brain.Reset();
            _hitPause = _deadTime = 0f;
            _direction = -1;
            _body.position = _spawn;
            _body.linearVelocity = Vector2.zero;
            _health.ResetHealth();
            weaponHitCollider.enabled = true;
            silhouette.enabled = true;
            silhouette.transform.localScale = Vector3.one;
        }

        private void OnDamaged(DamageResult2D result) { if (result.DidApply) _hitPause = 0.12f; }
        private void OnDied(DamageResult2D result)
        {
            _brain.Kill();
            _body.linearVelocity = Vector2.zero;
            weaponHitCollider.enabled = false;
            _hitPause = 0f;
        }
    }
}
