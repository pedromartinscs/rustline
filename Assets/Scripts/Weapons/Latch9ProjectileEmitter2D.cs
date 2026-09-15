using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    [DefaultExecutionOrder(130)]
    [DisallowMultipleComponent]
    public sealed class Latch9ProjectileEmitter2D : MonoBehaviour
    {
        private const string DefaultMetadataResourcePath = "Generated/Latch9MuzzleMetadata";

        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private PlayerLatch9AimPresenter2D latchPresenter;
        [SerializeField] private Latch9MuzzleMetadata2D metadata;

        private bool _hasPendingShot;
        private WeaponShotFired2D _pendingShot;

        public bool IsReady => weaponController != null && latchPresenter != null && ResolveMetadata() != null;
        public bool HasPendingShot => _hasPendingShot;

        public void Configure(
            PlayerWeaponController2D controller,
            PlayerLatch9AimPresenter2D presenter,
            Latch9MuzzleMetadata2D muzzleMetadata)
        {
            weaponController = controller;
            latchPresenter = presenter;
            metadata = muzzleMetadata;
            _hasPendingShot = false;
        }

        public bool QueueLaunch(in WeaponShotFired2D shot)
        {
            if (!IsReady || shot.Weapon == null ||
                shot.Weapon.DeliveryMode != WeaponDeliveryMode2D.Projectile)
            {
                return false;
            }

            _pendingShot = shot;
            _hasPendingShot = true;
            return true;
        }

        public void CancelPending()
        {
            _hasPendingShot = false;
        }

        private void LateUpdate()
        {
            if (!_hasPendingShot)
            {
                return;
            }

            WeaponShotFired2D shot = _pendingShot;
            _hasPendingShot = false;
            Launch(in shot);
        }

        private void Launch(in WeaponShotFired2D shot)
        {
            Vector2 origin = shot.Origin;
            SpriteRenderer armsRenderer = latchPresenter != null
                ? latchPresenter.ArmsWeaponSpriteRenderer
                : null;
            Latch9MuzzleMetadata2D resolvedMetadata = ResolveMetadata();

            if (latchPresenter != null && armsRenderer != null && resolvedMetadata != null &&
                latchPresenter.TryGetCurrentRenderedPose(out Latch9RenderedPose2D pose) &&
                Latch9MuzzleFlashMath.TryResolve(
                    resolvedMetadata,
                    in pose,
                    out Vector2 localPosition,
                    out _))
            {
                origin = armsRenderer.transform.TransformPoint(localPosition);
            }

            GameObject projectileObject = new GameObject(
                shot.ShotMode == WeaponShotMode2D.Bouncing
                    ? "Latch-9 Projectile Bouncing"
                    : "Latch-9 Projectile Conventional");
            projectileObject.transform.position = origin;
            LineRenderer lineRenderer = projectileObject.AddComponent<LineRenderer>();
            Latch9Projectile2D projectile = projectileObject.AddComponent<Latch9Projectile2D>();

            Material sharedMaterial = null;
            if (weaponController != null && weaponController.ShotFeedback != null &&
                weaponController.ShotFeedback.TraceRenderer != null)
            {
                sharedMaterial = weaponController.ShotFeedback.TraceRenderer.sharedMaterial;
            }

            int sortingLayerId = armsRenderer != null ? armsRenderer.sortingLayerID : 0;
            int sortingOrder = armsRenderer != null ? armsRenderer.sortingOrder + 2 : 0;
            Collider2D ownerCollider = weaponController != null
                ? weaponController.GetComponent<Collider2D>()
                : null;
            Transform ownerRoot = weaponController != null ? weaponController.transform : transform;
            LayerMask projectileHitLayers = weaponController != null
                ? weaponController.HitLayers
                : default;

            projectile.Initialize(
                weaponController,
                shot.Weapon,
                shot.ShotMode,
                origin,
                shot.Direction,
                projectileHitLayers,
                ownerCollider,
                ownerRoot,
                lineRenderer,
                sharedMaterial,
                sortingLayerId,
                sortingOrder);
        }

        private Latch9MuzzleMetadata2D ResolveMetadata()
        {
            if (metadata == null)
            {
                metadata = Resources.Load<Latch9MuzzleMetadata2D>(DefaultMetadataResourcePath);
            }

            return metadata;
        }
    }
}
