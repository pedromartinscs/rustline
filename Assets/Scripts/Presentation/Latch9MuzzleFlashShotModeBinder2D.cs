using Rustline.Gameplay.Weapons;
using UnityEngine;

namespace Rustline.Presentation
{
    [DisallowMultipleComponent]
    public sealed class Latch9MuzzleFlashShotModeBinder2D : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private Latch9MuzzleFlashPresenter2D muzzleFlash;

        public void Configure(
            PlayerWeaponController2D controller,
            Latch9MuzzleFlashPresenter2D presenter)
        {
            Unsubscribe();
            weaponController = controller;
            muzzleFlash = presenter;
            Apply(controller != null ? controller.CurrentShotMode : WeaponShotMode2D.Conventional);
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            Apply(weaponController != null
                ? weaponController.CurrentShotMode
                : WeaponShotMode2D.Conventional);
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (weaponController == null)
            {
                return;
            }
            weaponController.ShotModeChanged -= Apply;
            weaponController.ShotModeChanged += Apply;
        }

        private void Unsubscribe()
        {
            if (weaponController != null)
            {
                weaponController.ShotModeChanged -= Apply;
            }
        }

        private void Apply(WeaponShotMode2D mode)
        {
            muzzleFlash?.SetProfile(mode == WeaponShotMode2D.Bouncing
                ? Latch9MuzzleFlashProfile2D.Bouncing
                : Latch9MuzzleFlashProfile2D.Conventional);
        }
    }
}
