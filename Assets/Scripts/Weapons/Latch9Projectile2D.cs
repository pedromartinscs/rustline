using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class Latch9Projectile2D : MonoBehaviour
    {
        public const float VisualLength = 4f / 16f;
        public const float VisualWidth = 1f / 16f;
        public const int ConventionalPaletteIndex = 20;
        public const int BouncingPaletteIndex = 22;
        public const float PostBounceSeparation = 0.25f / 16f;
        public const float ImmediateRehitGuardDistance = 0.5f / 16f;
        private const float RangeEpsilon = 1f / 1024f;
        private const int MaxInteractionsPerFrame = 8;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
        private PlayerWeaponController2D _controller;
        private WeaponDefinition2D _definition;
        private WeaponShotMode2D _shotMode;
        private Vector2 _initialOrigin;
        private Vector2 _initialDirection;
        private Vector2 _position;
        private Vector2 _direction;
        private float _remainingRange;
        private float _travelledDistance;
        private float _distanceSinceBounce;
        private int _bounceCount;
        private LayerMask _hitLayers;
        private Collider2D _ownerCollider;
        private Collider2D _lastBounceCollider;
        private Transform _ownerRoot;
        private ContactFilter2D _hitFilter;
        private LineRenderer _renderer;
        private bool _initialized;
        private bool _completed;

        public WeaponShotMode2D ShotMode => _shotMode;
        public int BounceCount => _bounceCount;
        public Vector2 Position => _position;
        public Vector2 Direction => _direction;
        public float TravelledDistance => _travelledDistance;

        public static Color32 ResolveVisualColor(WeaponShotMode2D mode)
        {
            return RustlinePalette.GetColor(
                mode == WeaponShotMode2D.Bouncing
                    ? BouncingPaletteIndex
                    : ConventionalPaletteIndex);
        }

        public static bool ShouldIgnoreImmediateRehit(
            Collider2D candidateCollider,
            Collider2D lastBounceCollider,
            float hitDistance,
            float distanceSinceBounce)
        {
            return candidateCollider != null &&
                   lastBounceCollider != null &&
                   candidateCollider == lastBounceCollider &&
                   hitDistance <= ImmediateRehitGuardDistance &&
                   distanceSinceBounce <= ImmediateRehitGuardDistance;
        }

        public void Initialize(
            PlayerWeaponController2D controller,
            WeaponDefinition2D definition,
            WeaponShotMode2D shotMode,
            Vector2 origin,
            Vector2 direction,
            LayerMask hitLayers,
            Collider2D ownerCollider,
            Transform ownerRoot,
            LineRenderer lineRenderer,
            Material sharedMaterial,
            int sortingLayerId,
            int sortingOrder)
        {
            _controller = controller;
            _definition = definition;
            _shotMode = shotMode;
            _initialOrigin = origin;
            _initialDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            _position = origin;
            _direction = _initialDirection;
            _remainingRange = definition != null ? definition.Range : 0f;
            _travelledDistance = 0f;
            _distanceSinceBounce = 0f;
            _bounceCount = 0;
            _hitLayers = hitLayers;
            _ownerCollider = ownerCollider;
            _lastBounceCollider = null;
            _ownerRoot = ownerRoot;
            _renderer = lineRenderer != null ? lineRenderer : gameObject.AddComponent<LineRenderer>();
            _completed = false;

            _hitFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _hitLayers,
                useTriggers = true,
            };

            ConfigureRenderer(sharedMaterial, sortingLayerId, sortingOrder);
            transform.position = origin;
            UpdateVisual();
            _initialized = controller != null && definition != null && definition.ProjectileSpeed > 0f;
            if (!_initialized)
            {
                enabled = false;
                if (_renderer != null)
                {
                    _renderer.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (!_initialized || _completed || _definition == null)
            {
                return;
            }

            float distanceBudget = _definition.ProjectileSpeed * Time.deltaTime;
            if (distanceBudget <= 0f)
            {
                return;
            }

            Advance(distanceBudget);
        }

        private void Advance(float distanceBudget)
        {
            int interactionCount = 0;
            while (!_completed && distanceBudget > 0f && _remainingRange > 0f &&
                   interactionCount++ < MaxInteractionsPerFrame)
            {
                float segmentBudget = Mathf.Min(distanceBudget, _remainingRange);
                if (!TryFindNearestHit(_position, _direction, segmentBudget, out RaycastHit2D hit))
                {
                    Move(segmentBudget);
                    distanceBudget -= segmentBudget;
                    if (_remainingRange <= RangeEpsilon)
                    {
                        CompleteAtRangeLimit();
                    }
                    continue;
                }

                float hitDistance = Mathf.Max(0f, hit.distance);
                if (hitDistance > 0f)
                {
                    Move(hitDistance);
                    distanceBudget = Mathf.Max(0f, distanceBudget - hitDistance);
                }
                else
                {
                    _position = hit.point;
                    transform.position = _position;
                }

                Collider2D hitCollider = hit.collider;
                int damage = _definition.ResolveDamage(_shotMode, _bounceCount);
                IWeaponCombatTarget2D combatTarget = hitCollider != null
                    ? hitCollider.GetComponent<IWeaponCombatTarget2D>()
                    : null;

                if (combatTarget != null)
                {
                    var hitInfo = new WeaponHitInfo2D(
                        _definition,
                        _initialOrigin,
                        _direction,
                        hit.point,
                        hit.normal,
                        _travelledDistance,
                        damage,
                        hitCollider);
                    combatTarget.ReceiveHit(in hitInfo);
                    Complete(
                        true,
                        hitCollider,
                        hit.normal,
                        damage,
                        true,
                        hit.point);
                    return;
                }

                if (!WeaponBounceMath2D.CanReflect(
                        _shotMode,
                        _bounceCount,
                        _definition.MaxBounces) ||
                    _remainingRange <= RangeEpsilon)
                {
                    Complete(
                        true,
                        hitCollider,
                        hit.normal,
                        damage,
                        false,
                        hit.point);
                    return;
                }

                Vector2 bounceNormal = hit.normal.sqrMagnitude > 0f
                    ? hit.normal.normalized
                    : -_direction;
                _direction = WeaponBounceMath2D.Reflect(_direction, bounceNormal);
                _lastBounceCollider = hitCollider;
                _position = hit.point +
                            bounceNormal * PostBounceSeparation +
                            _direction * PostBounceSeparation;
                transform.position = _position;
                _distanceSinceBounce = 0f;
                _bounceCount++;
                UpdateVisual();
            }
        }

        private void Move(float distance)
        {
            if (distance <= 0f)
            {
                return;
            }

            _position += _direction * distance;
            _travelledDistance += distance;
            _distanceSinceBounce += distance;
            _remainingRange = Mathf.Max(0f, _remainingRange - distance);
            transform.position = _position;
            UpdateVisual();
        }

        private void CompleteAtRangeLimit()
        {
            int damage = _definition.ResolveDamage(_shotMode, _bounceCount);
            Complete(false, null, Vector2.zero, damage, false, _position);
        }

        private void Complete(
            bool hit,
            Collider2D hitCollider,
            Vector2 hitNormal,
            int damage,
            bool receiverNotified,
            Vector2 endPoint)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }

            var result = new WeaponShotResult2D(
                _definition,
                _shotMode,
                _initialOrigin,
                _initialDirection,
                _direction,
                endPoint,
                hit,
                hitCollider,
                hitNormal,
                _travelledDistance,
                damage,
                receiverNotified,
                _bounceCount);
            _controller?.ReportProjectileResolved(result);
            Destroy(gameObject);
        }

        private bool TryFindNearestHit(
            Vector2 origin,
            Vector2 direction,
            float range,
            out RaycastHit2D nearestHit)
        {
            int hitCount = Physics2D.Raycast(origin, direction, _hitFilter, _hits, range);
            nearestHit = default;
            float nearestDistance = float.PositiveInfinity;
            bool found = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D candidate = _hits[index];
                if (candidate.distance >= nearestDistance)
                {
                    continue;
                }

                Collider2D candidateCollider = candidate.collider;
                if (candidateCollider == null || candidateCollider == _ownerCollider ||
                    (_ownerRoot != null && candidateCollider.transform.IsChildOf(_ownerRoot)) ||
                    candidateCollider.GetComponent<WeaponRaycastPassthrough2D>() != null ||
                    ShouldIgnoreImmediateRehit(
                        candidateCollider,
                        _lastBounceCollider,
                        candidate.distance,
                        _distanceSinceBounce))
                {
                    continue;
                }

                nearestHit = candidate;
                nearestDistance = candidate.distance;
                found = true;
            }

            return found;
        }

        private void ConfigureRenderer(Material sharedMaterial, int sortingLayerId, int sortingOrder)
        {
            _renderer.useWorldSpace = true;
            _renderer.positionCount = 2;
            _renderer.startWidth = VisualWidth;
            _renderer.endWidth = VisualWidth;
            _renderer.numCapVertices = 0;
            _renderer.numCornerVertices = 0;
            _renderer.textureMode = LineTextureMode.Stretch;
            _renderer.alignment = LineAlignment.TransformZ;
            _renderer.sortingLayerID = sortingLayerId;
            _renderer.sortingOrder = sortingOrder;
            if (sharedMaterial != null)
            {
                _renderer.sharedMaterial = sharedMaterial;
            }

            Color color = ResolveVisualColor(_shotMode);
            _renderer.startColor = color;
            _renderer.endColor = color;
            _renderer.enabled = true;
        }

        private void UpdateVisual()
        {
            if (_renderer == null)
            {
                return;
            }

            float visibleLength = Mathf.Min(VisualLength, _distanceSinceBounce);
            Vector2 tail = _position - _direction * visibleLength;
            _renderer.SetPosition(0, tail);
            _renderer.SetPosition(1, _position);
        }
    }
}
