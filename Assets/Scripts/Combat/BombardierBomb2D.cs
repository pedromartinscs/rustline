using System;
using System.Collections.Generic;
using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class BombardierBomb2D : MonoBehaviour
    {
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        private Bombardier2D _owner;
        private CombatHealth2D _playerHealth;
        private CapsuleCollider2D _playerCollider;
        private LineRenderer _line;
        private Material _material;
        private IReadOnlyList<Sprite> _explosionFrames;
        private BombardierExplosionPresenter2D _explosion;
        private Vector2 _origin;
        private Vector2 _velocity;
        private float _age;
        private bool _launched;
        private ContactFilter2D _contactFilter;

        public bool HasExploded { get; private set; }
        public bool TimedOut { get; private set; }
        public Vector2 ImpactPoint { get; private set; }
        public Collider2D FirstContactCollider { get; private set; }
        public Vector2 InitialVelocity => _velocity;
        public float Age => _age;
        public BombardierExplosionPresenter2D ExplosionVisual => _explosion;

        public void Launch(Bombardier2D owner, Vector2 origin, Vector2 target,
            CombatHealth2D playerHealth, CapsuleCollider2D playerCollider, Material sharedMaterial,
            IReadOnlyList<Sprite> explosionFrames)
        {
            if (explosionFrames == null || explosionFrames.Count != BombardierExplosionPresenter2D.FrameCount)
                throw new ArgumentException("Bombardier bomb requires six explosion sprites.", nameof(explosionFrames));
            _explosionFrames = explosionFrames;
            _material = sharedMaterial;
            _owner = owner;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _origin = origin;
            _velocity = BombardierBallistics2D.InitialVelocity(origin, target);
            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = (1 << 6) | (1 << 9),
                useTriggers = false,
            };
            _line = gameObject.AddComponent<LineRenderer>();
            _line.sharedMaterial = sharedMaterial;
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.widthMultiplier = 0.0625f;
            _line.sortingOrder = 32;
            SetCircle(BombardierBallistics2D.CollisionRadius, 8,
                RustlinePalette.GetColor(13));
            _launched = true;
        }

        private void FixedUpdate()
        {
            if (!_launched) return;
            if (HasExploded)
            {
                // Spent and inert. Only the presentation clock controls its remaining lifetime.
                if (_explosion == null || _explosion.IsComplete) Destroy(gameObject);
                return;
            }

            float nextAge = _age + Time.fixedDeltaTime;
            if (nextAge >= BombardierBallistics2D.SafetyLifetime)
            {
                TimedOut = true;
                _launched = false;
                Destroy(gameObject);
                return;
            }
            Vector2 previous = BombardierBallistics2D.Position(_origin, _velocity, _age);
            Vector2 next = BombardierBallistics2D.Position(_origin, _velocity, nextAge);
            Vector2 segment = next - previous;
            float distance = segment.magnitude;
            int count = distance > 0f
                ? Physics2D.CircleCast(previous, BombardierBallistics2D.CollisionRadius,
                    segment / distance, _contactFilter, _hits, distance)
                : 0;
            RaycastHit2D nearest = default;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < count; index++)
            {
                if (_hits[index].collider != null && _hits[index].distance < nearestDistance)
                {
                    nearest = _hits[index];
                    nearestDistance = nearest.distance;
                }
            }
            _age = nextAge;
            if (nearest.collider != null)
            {
                Explode(nearest.point, nearest.normal, nearest.collider);
                return;
            }
            transform.position = next;
        }

        public void CancelForReset()
        {
            _launched = false;
            enabled = false;
            if (_line != null) _line.enabled = false;
            if (_explosion != null) _explosion.Cancel();
            Destroy(gameObject);
        }

        private void Explode(Vector2 point, Vector2 normal, Collider2D directCollider)
        {
            if (HasExploded) return;
            HasExploded = true;
            FirstContactCollider = directCollider;
            ImpactPoint = point;
            transform.position = point;
            _line.enabled = false; // Keep only the flight body; never draw the old circular blast.
            var visual = new GameObject("Explosion Visual");
            visual.transform.SetParent(transform, false);
            _explosion = visual.AddComponent<BombardierExplosionPresenter2D>();
            _explosion.Play(_explosionFrames, _material, point);

            // Must remain in this detonation tick. A lethal hit may synchronously cancel
            // this owned visual through the existing player-death/reset cleanup path.
            BombardierExplosion2D.TryDamagePlayer(point, normal, directCollider,
                _playerHealth, _playerCollider, this);
        }

        private void SetCircle(float radius, int segments, Color color)
        {
            _line.positionCount = segments;
            _line.startColor = color;
            _line.endColor = color;
            for (int index = 0; index < segments; index++)
            {
                float angle = 2f * Mathf.PI * index / segments;
                _line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void OnDestroy()
        {
            if (_owner != null) _owner.UnregisterBomb(this);
        }
    }
}
