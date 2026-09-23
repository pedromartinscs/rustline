using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    public enum WeaponFireMode2D
    {
        SemiAutomatic,
        Automatic,
    }

    public enum WeaponShotMode2D
    {
        Conventional,
        Bouncing,
    }

    public enum WeaponDeliveryMode2D
    {
        Hitscan,
        Projectile,
    }

    public enum WeaponAmmoPolicy2D
    {
        Untracked,
        Infinite,
        Magazine,
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Rustline/Weapon Definition 2D")]
    public sealed class WeaponDefinition2D : ScriptableObject
    {
        [SerializeField] private string weaponId = "longwatch_dmr";
        [SerializeField] private string displayName = "Longwatch DMR";
        [SerializeField] private WeaponFireMode2D fireMode = WeaponFireMode2D.SemiAutomatic;
        [SerializeField] private WeaponFireMode2D[] supportedFireModes =
        {
            WeaponFireMode2D.SemiAutomatic,
        };
        [SerializeField] private WeaponShotMode2D shotMode = WeaponShotMode2D.Conventional;
        [SerializeField] private WeaponShotMode2D[] supportedShotModes =
        {
            WeaponShotMode2D.Conventional,
        };
        [SerializeField] private WeaponDeliveryMode2D deliveryMode = WeaponDeliveryMode2D.Hitscan;
        [SerializeField, Min(0f)] private float projectileSpeed;
        [SerializeField, Min(0.01f)] private float shotInterval = 1f / 12f;
        [SerializeField, Min(0.01f)] private float range = 80f;
        [SerializeField, Min(0)] private int damage = 40;
        [SerializeField, Min(0)] private int maxBounces;
        [SerializeField, Range(1, 100)] private int bouncingInitialDamagePercent = 80;
        [SerializeField, Range(1, 100)] private int bouncingDamageStepPercent = 20;
        [SerializeField] private WeaponAmmoPolicy2D ammoPolicy = WeaponAmmoPolicy2D.Untracked;
        [SerializeField, Min(1)] private int magazineCapacity = 1;
        [SerializeField, Min(0)] private int initialReserveMagazines;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public WeaponFireMode2D FireMode => fireMode;
        public int SupportedFireModeCount => supportedFireModes?.Length ?? 0;
        public bool SupportsMultipleFireModes => SupportedFireModeCount > 1;
        public WeaponShotMode2D ShotMode => shotMode;
        public int SupportedShotModeCount => supportedShotModes?.Length ?? 0;
        public bool SupportsMultipleShotModes => SupportedShotModeCount > 1;
        public WeaponDeliveryMode2D DeliveryMode => deliveryMode;
        public float ProjectileSpeed => projectileSpeed;
        public float ShotInterval => shotInterval;
        public float Range => range;
        public int Damage => damage;
        public int MaxBounces => maxBounces;
        public int BouncingInitialDamagePercent => bouncingInitialDamagePercent;
        public int BouncingDamageStepPercent => bouncingDamageStepPercent;
        public WeaponAmmoPolicy2D AmmoPolicy => ammoPolicy;
        public int MagazineCapacity => magazineCapacity;
        public int InitialReserveMagazines => initialReserveMagazines;

        public bool SupportsFireMode(WeaponFireMode2D mode)
        {
            if (supportedFireModes == null)
            {
                return false;
            }

            for (int i = 0; i < supportedFireModes.Length; i++)
            {
                if (supportedFireModes[i] == mode)
                {
                    return true;
                }
            }

            return false;
        }

        public WeaponFireMode2D GetNextSupportedFireMode(WeaponFireMode2D current)
        {
            if (supportedFireModes == null || supportedFireModes.Length == 0)
            {
                return fireMode;
            }

            for (int i = 0; i < supportedFireModes.Length; i++)
            {
                if (supportedFireModes[i] == current)
                {
                    return supportedFireModes[(i + 1) % supportedFireModes.Length];
                }
            }

            return supportedFireModes[0];
        }

        public bool SupportsShotMode(WeaponShotMode2D mode)
        {
            if (supportedShotModes == null)
            {
                return false;
            }

            for (int i = 0; i < supportedShotModes.Length; i++)
            {
                if (supportedShotModes[i] == mode)
                {
                    return true;
                }
            }

            return false;
        }

        public WeaponShotMode2D GetNextSupportedShotMode(WeaponShotMode2D current)
        {
            if (supportedShotModes == null || supportedShotModes.Length == 0)
            {
                return shotMode;
            }

            for (int i = 0; i < supportedShotModes.Length; i++)
            {
                if (supportedShotModes[i] == current)
                {
                    return supportedShotModes[(i + 1) % supportedShotModes.Length];
                }
            }

            return supportedShotModes[0];
        }

        public int ResolveDamage(WeaponShotMode2D mode, int bounceCount)
        {
            if (mode != WeaponShotMode2D.Bouncing)
            {
                return damage;
            }

            int clampedBounceCount = Mathf.Clamp(bounceCount, 0, maxBounces);
            int percent = bouncingInitialDamagePercent -
                          bouncingDamageStepPercent * clampedBounceCount;
            return Mathf.Max(1, Mathf.RoundToInt(damage * percent / 100f));
        }

        public bool IsSane(out string reason)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Weapon ID and display name are required.";
                return false;
            }

            if (supportedFireModes == null || supportedFireModes.Length == 0)
            {
                reason = "A weapon must support at least one fire mode.";
                return false;
            }

            if (!SupportsFireMode(fireMode))
            {
                reason = "The default fire mode must be present in the supported fire-mode list.";
                return false;
            }

            for (int i = 0; i < supportedFireModes.Length; i++)
            {
                for (int j = i + 1; j < supportedFireModes.Length; j++)
                {
                    if (supportedFireModes[i] == supportedFireModes[j])
                    {
                        reason = "Supported fire modes must not contain duplicates.";
                        return false;
                    }
                }
            }

            if (supportedShotModes == null || supportedShotModes.Length == 0)
            {
                reason = "A weapon must support at least one shot mode.";
                return false;
            }

            if (!SupportsShotMode(shotMode))
            {
                reason = "The default shot mode must be present in the supported shot-mode list.";
                return false;
            }

            for (int i = 0; i < supportedShotModes.Length; i++)
            {
                for (int j = i + 1; j < supportedShotModes.Length; j++)
                {
                    if (supportedShotModes[i] == supportedShotModes[j])
                    {
                        reason = "Supported shot modes must not contain duplicates.";
                        return false;
                    }
                }
            }

            if (shotInterval <= 0f || range <= 0f || damage <= 0)
            {
                reason = "Weapon fire interval, range, and damage must be positive.";
                return false;
            }

            if (ammoPolicy == WeaponAmmoPolicy2D.Magazine &&
                (magazineCapacity <= 0 || initialReserveMagazines < 0))
            {
                reason = "Magazine capacity must be positive and reserve count cannot be negative.";
                return false;
            }

            if (deliveryMode == WeaponDeliveryMode2D.Projectile && projectileSpeed <= 0f)
            {
                reason = "A projectile weapon must have a positive projectile speed.";
                return false;
            }

            if (SupportsShotMode(WeaponShotMode2D.Bouncing))
            {
                if (maxBounces <= 0)
                {
                    reason = "A bouncing weapon must allow at least one bounce.";
                    return false;
                }

                if (bouncingInitialDamagePercent <= 0 || bouncingInitialDamagePercent > 100 ||
                    bouncingDamageStepPercent <= 0 || bouncingDamageStepPercent > 100)
                {
                    reason = "Bouncing damage percentages must be within 1..100.";
                    return false;
                }

                if (bouncingInitialDamagePercent - bouncingDamageStepPercent * maxBounces <= 0)
                {
                    reason = "Bouncing damage must remain positive through the configured maximum bounce count.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
