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

        public event Action<WeaponShotResult2D> ShotResolved;

        public WeaponDefinition2D WeaponDefinition => weaponDefinition;
        public LayerMask HitLayers => hitLayers;
        public PrototypeWeaponShotFeedback2D ShotFeedback => shotFeedback;
        public int ShotCount { get; private set; }
        public WeaponShotResult2D LastShotResult { get; private set; }

        private void Awake()
        {
            _playerCollider = GetComponent<Collider2D>();
            RebuildHitFilter();
        }

        private void OnValidate()
        {
            RebuildHitFilter();
        }

        private void Update()
        {
            if (input == null || !input.ConsumeFirePressed())
            {
                return;
            }

            TryFire(Time.time);
        }

        public bool TryFire(float currentTime)
        {
            using (FireMarker.Auto())
            {
                if (weaponDefinition == null || !weaponDefinition.IsSane(out _) ||
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
                ResolveHitscan(origin, direction, out WeaponShotResult2D result);
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

        private void ResolveHitscan(Vector2 origin, Vector2 direction, out WeaponShotResult2D result)
        {
            int hitCount = Physics2D.Raycast(
                origin,
                direction,
                _hitFilter,
                _hits,
                weaponDefinition.Range);
            RaycastHit2D nearestHit = default;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D candidate = _hits[index];
                if (candidate.collider == null || candidate.collider == _playerCollider ||
                    candidate.collider.transform.IsChildOf(transform) || candidate.distance >= nearestDistance)
                {
                    continue;
                }

                nearestHit = candidate;
                nearestDistance = candidate.distance;
            }

            if (nearestHit.collider == null)
            {
                result = new WeaponShotResult2D(
                    weaponDefinition,
                    origin,
                    direction,
                    origin + direction * weaponDefinition.Range,
                    false,
                    null,
                    Vector2.zero,
                    weaponDefinition.Range,
                    weaponDefinition.Damage,
                    false);
                return;
            }

            var hitInfo = new WeaponHitInfo2D(
                weaponDefinition,
                origin,
                direction,
                nearestHit.point,
                nearestHit.normal,
                nearestHit.distance,
                weaponDefinition.Damage,
                nearestHit.collider);
            IWeaponHitReceiver2D receiver = nearestHit.collider.GetComponent<IWeaponHitReceiver2D>();
            bool receiverNotified = receiver != null;
            receiver?.ReceiveHit(in hitInfo);
            result = new WeaponShotResult2D(
                weaponDefinition,
                origin,
                direction,
                nearestHit.point,
                true,
                nearestHit.collider,
                nearestHit.normal,
                nearestHit.distance,
                weaponDefinition.Damage,
                receiverNotified);
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
