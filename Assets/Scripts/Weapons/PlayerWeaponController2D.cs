using System;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using Unity.Profiling;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    /// <summary>
    /// Consumes current-frame aim and locomotion state after PlayerAim2D and
    /// PlayerAnimator2D have updated. Authored ten-degree poses never affect hitscan direction.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerAim2D), typeof(PlayerMotor2D))]
    [RequireComponent(typeof(PlayerAnimator2D), typeof(Collider2D))]
    public sealed class PlayerWeaponController2D : MonoBehaviour
    {
        private const float BounceSurfaceEpsilon = 1f / 1024f;
        private static readonly ProfilerMarker FireMarker = new ProfilerMarker("Rustline.Weapon.Fire");

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAim2D playerAim;
        [SerializeField] private PlayerAnimator2D playerAnimator;
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private WeaponDefinition2D weaponDefinition;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private PrototypeWeaponShotFeedback2D shotFeedback;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
        private readonly SemiAutomaticWeaponCooldown2D _cooldown = new SemiAutomaticWeaponCooldown2D();
        private Collider2D _playerCollider;
        private ContactFilter2D _hitFilter;
        private WeaponFireMode2D _currentFireMode = WeaponFireMode2D.SemiAutomatic;
        private WeaponShotMode2D _currentShotMode = WeaponShotMode2D.Conventional;

        public event Action<WeaponShotResult2D> ShotResolved;
        public event Action<WeaponFireMode2D> FireModeChanged;
        public event Action<WeaponShotMode2D> ShotModeChanged;

        public WeaponDefinition2D WeaponDefinition => weaponDefinition;
        public WeaponFireMode2D CurrentFireMode => _currentFireMode;
        public WeaponShotMode2D CurrentShotMode => _currentShotMode;
        public LayerMask HitLayers => hitLayers;
        public PrototypeWeaponShotFeedback2D ShotFeedback => shotFeedback;
        public int ShotCount { get; private set; }
        public WeaponShotResult2D LastShotResult { get; private set; }

        private void Awake()
        {
            _playerCollider = GetComponent<Collider2D>();
            ApplyDefinitionDefaults(false);
            RebuildHitFilter();
        }

        private void OnValidate()
        {
            RebuildHitFilter();
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            if (input.ConsumeToggleFireModePressed())
            {
                // Latch-9 uses the existing right-click action for Conventional/Bouncing.
                // Weapons without multiple shot modes retain the established fire-mode toggle.
                if (weaponDefinition != null && weaponDefinition.SupportsMultipleShotModes)
                {
                    CycleShotMode();
                }
                else
                {
                    CycleFireMode();
                }
            }

            // Always consume the press edge even in automatic mode so switching back to semi-auto
            // while the button is still held cannot replay an old click.
            bool firePressed = input.ConsumeFirePressed();
            bool wantsFire = _currentFireMode == WeaponFireMode2D.Automatic
                ? input.IsFireHeld
                : firePressed;

            if (wantsFire)
            {
                TryFire(Time.time);
            }
        }

        public void EquipWeapon(WeaponDefinition2D definition)
        {
            weaponDefinition = definition;
            ApplyDefinitionDefaults(true);
            _cooldown.Reset();
            input?.ClearTransientState();
            shotFeedback?.Hide();
        }

        public bool TryFire(float currentTime)
        {
            using (FireMarker.Auto())
            {
                if (weaponDefinition == null || !weaponDefinition.IsSane(out _) ||
                    !weaponDefinition.SupportsFireMode(_currentFireMode) ||
                    !weaponDefinition.SupportsShotMode(_currentShotMode) ||
                    playerAim == null || !playerAim.HasValidAim ||
                    playerAnimator == null || playerMotor == null ||
                    !WeaponFirePolicy2D.CanFire(
                        playerAnimator.CurrentState,
                        playerMotor.IsWallBraced,
                        playerMotor.IsWallKicking) ||
                    !_cooldown.TryConsume(currentTime, weaponDefinition.ShotInterval))
                {
                    return false;
                }

                Vector2 origin = playerAim.AimOriginWorld;
                Vector2 direction = playerAim.ContinuousAimDirection;
                ResolveShot(origin, direction, out WeaponShotResult2D result);
                LastShotResult = result;
                ShotCount++;
                shotFeedback?.Show(result);
                ShotResolved?.Invoke(result);
                return true;
            }
        }

        public void ResetTransientState()
        {
            _cooldown.Reset();
            input?.ClearTransientState();
            shotFeedback?.Hide();
        }

        private void ApplyDefinitionDefaults(bool notify)
        {
            WeaponFireMode2D nextFireMode = weaponDefinition != null
                ? weaponDefinition.FireMode
                : WeaponFireMode2D.SemiAutomatic;
            WeaponShotMode2D nextShotMode = weaponDefinition != null
                ? weaponDefinition.ShotMode
                : WeaponShotMode2D.Conventional;

            bool fireModeChanged = _currentFireMode != nextFireMode;
            bool shotModeChanged = _currentShotMode != nextShotMode;
            _currentFireMode = nextFireMode;
            _currentShotMode = nextShotMode;

            if (notify && fireModeChanged)
            {
                FireModeChanged?.Invoke(_currentFireMode);
            }

            if (notify && shotModeChanged)
            {
                ShotModeChanged?.Invoke(_currentShotMode);
            }
        }

        private void CycleFireMode()
        {
            if (weaponDefinition == null || !weaponDefinition.SupportsMultipleFireModes)
            {
                return;
            }

            WeaponFireMode2D nextMode = weaponDefinition.GetNextSupportedFireMode(_currentFireMode);
            if (nextMode == _currentFireMode)
            {
                return;
            }

            _currentFireMode = nextMode;
            FireModeChanged?.Invoke(_currentFireMode);
        }

        private void CycleShotMode()
        {
            if (weaponDefinition == null || !weaponDefinition.SupportsMultipleShotModes)
            {
                return;
            }

            WeaponShotMode2D nextMode = weaponDefinition.GetNextSupportedShotMode(_currentShotMode);
            if (nextMode == _currentShotMode)
            {
                return;
            }

            _currentShotMode = nextMode;
            ShotModeChanged?.Invoke(_currentShotMode);
        }

        private void ResolveShot(Vector2 origin, Vector2 direction, out WeaponShotResult2D result)
        {
            if (_currentShotMode == WeaponShotMode2D.Bouncing)
            {
                ResolveBouncingHitscan(origin, direction, out result);
                return;
            }

            ResolveConventionalHitscan(origin, direction, out result);
        }

        private void ResolveConventionalHitscan(
            Vector2 origin,
            Vector2 direction,
            out WeaponShotResult2D result)
        {
            int damage = weaponDefinition.ResolveDamage(WeaponShotMode2D.Conventional, 0);
            if (!TryFindNearestHit(origin, direction, weaponDefinition.Range, out RaycastHit2D nearestHit))
            {
                result = new WeaponShotResult2D(
                    weaponDefinition,
                    WeaponShotMode2D.Conventional,
                    origin,
                    direction,
                    direction,
                    origin + direction * weaponDefinition.Range,
                    false,
                    null,
                    Vector2.zero,
                    weaponDefinition.Range,
                    damage,
                    false,
                    0);
                return;
            }

            Collider2D nearestCollider = nearestHit.collider;
            var hitInfo = new WeaponHitInfo2D(
                weaponDefinition,
                origin,
                direction,
                nearestHit.point,
                nearestHit.normal,
                nearestHit.distance,
                damage,
                nearestCollider);
            IWeaponHitReceiver2D receiver = nearestCollider.GetComponent<IWeaponHitReceiver2D>();
            bool receiverNotified = receiver != null;
            receiver?.ReceiveHit(in hitInfo);
            result = new WeaponShotResult2D(
                weaponDefinition,
                WeaponShotMode2D.Conventional,
                origin,
                direction,
                direction,
                nearestHit.point,
                true,
                nearestCollider,
                nearestHit.normal,
                nearestHit.distance,
                damage,
                receiverNotified,
                0);
        }

        private void ResolveBouncingHitscan(
            Vector2 origin,
            Vector2 direction,
            out WeaponShotResult2D result)
        {
            Vector2 initialDirection = direction;
            Vector2 segmentDirection = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            Vector2 segmentOrigin = origin;
            float remainingRange = weaponDefinition.Range;
            float travelledDistance = 0f;
            int bounceCount = 0;

            while (remainingRange > 0f)
            {
                int damage = weaponDefinition.ResolveDamage(WeaponShotMode2D.Bouncing, bounceCount);
                if (!TryFindNearestHit(
                        segmentOrigin,
                        segmentDirection,
                        remainingRange,
                        out RaycastHit2D nearestHit))
                {
                    result = new WeaponShotResult2D(
                        weaponDefinition,
                        WeaponShotMode2D.Bouncing,
                        origin,
                        initialDirection,
                        segmentDirection,
                        segmentOrigin + segmentDirection * remainingRange,
                        false,
                        null,
                        Vector2.zero,
                        travelledDistance + remainingRange,
                        damage,
                        false,
                        bounceCount);
                    return;
                }

                Collider2D nearestCollider = nearestHit.collider;
                travelledDistance += nearestHit.distance;
                remainingRange = Mathf.Max(0f, remainingRange - nearestHit.distance);

                IWeaponHitReceiver2D receiver = nearestCollider.GetComponent<IWeaponHitReceiver2D>();
                if (receiver != null)
                {
                    var hitInfo = new WeaponHitInfo2D(
                        weaponDefinition,
                        origin,
                        segmentDirection,
                        nearestHit.point,
                        nearestHit.normal,
                        travelledDistance,
                        damage,
                        nearestCollider);
                    receiver.ReceiveHit(in hitInfo);
                    result = new WeaponShotResult2D(
                        weaponDefinition,
                        WeaponShotMode2D.Bouncing,
                        origin,
                        initialDirection,
                        segmentDirection,
                        nearestHit.point,
                        true,
                        nearestCollider,
                        nearestHit.normal,
                        travelledDistance,
                        damage,
                        true,
                        bounceCount);
                    return;
                }

                // Receiverless geometry (including current Ground) is collision-only:
                // it can reflect the Latch-9 shot but is never mutated or damaged here.
                if (bounceCount >= weaponDefinition.MaxBounces || remainingRange <= BounceSurfaceEpsilon)
                {
                    result = new WeaponShotResult2D(
                        weaponDefinition,
                        WeaponShotMode2D.Bouncing,
                        origin,
                        initialDirection,
                        segmentDirection,
                        nearestHit.point,
                        true,
                        nearestCollider,
                        nearestHit.normal,
                        travelledDistance,
                        damage,
                        false,
                        bounceCount);
                    return;
                }

                Vector2 bounceNormal = nearestHit.normal.sqrMagnitude > 0f
                    ? nearestHit.normal.normalized
                    : -segmentDirection;
                segmentDirection = WeaponBounceMath2D.Reflect(segmentDirection, bounceNormal);
                segmentOrigin = nearestHit.point +
                                bounceNormal * BounceSurfaceEpsilon +
                                segmentDirection * BounceSurfaceEpsilon;
                bounceCount++;
            }

            int finalDamage = weaponDefinition.ResolveDamage(WeaponShotMode2D.Bouncing, bounceCount);
            result = new WeaponShotResult2D(
                weaponDefinition,
                WeaponShotMode2D.Bouncing,
                origin,
                initialDirection,
                segmentDirection,
                segmentOrigin,
                false,
                null,
                Vector2.zero,
                travelledDistance,
                finalDamage,
                false,
                bounceCount);
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
                if (candidateCollider == null || candidateCollider == _playerCollider ||
                    candidateCollider.transform.IsChildOf(transform) ||
                    candidateCollider.GetComponent<WeaponRaycastPassthrough2D>() != null)
                {
                    continue;
                }

                nearestHit = candidate;
                nearestDistance = candidate.distance;
                found = true;
            }

            return found;
        }

        private void RebuildHitFilter()
        {
            _hitFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = hitLayers,
                useTriggers = true,
            };
        }
    }
}
