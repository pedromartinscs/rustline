using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    public enum WeaponFireMode2D
    {
        SemiAutomatic,
        Automatic,
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
        [SerializeField, Min(0.01f)] private float shotInterval = 1f / 12f;
        [SerializeField, Min(0.01f)] private float range = 80f;
        [SerializeField, Min(0)] private int damage = 40;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public WeaponFireMode2D FireMode => fireMode;
        public int SupportedFireModeCount => supportedFireModes?.Length ?? 0;
        public bool SupportsMultipleFireModes => SupportedFireModeCount > 1;
        public float ShotInterval => shotInterval;
        public float Range => range;
        public int Damage => damage;

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

            if (shotInterval <= 0f || range <= 0f || damage <= 0)
            {
                reason = "Weapon fire interval, range, and damage must be positive.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
