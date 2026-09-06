using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    public enum WeaponFireMode2D
    {
        SemiAutomatic,
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Rustline/Weapon Definition 2D")]
    public sealed class WeaponDefinition2D : ScriptableObject
    {
        [SerializeField] private string weaponId = "longwatch_dmr";
        [SerializeField] private string displayName = "Longwatch DMR";
        [SerializeField] private WeaponFireMode2D fireMode = WeaponFireMode2D.SemiAutomatic;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.25f;
        [SerializeField, Min(0.01f)] private float range = 80f;
        [SerializeField, Min(0)] private int damage = 40;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public WeaponFireMode2D FireMode => fireMode;
        public float ShotInterval => shotInterval;
        public float Range => range;
        public int Damage => damage;

        public bool IsSane(out string reason)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Weapon ID and display name are required.";
                return false;
            }

            if (fireMode != WeaponFireMode2D.SemiAutomatic || shotInterval <= 0f || range <= 0f || damage <= 0)
            {
                reason = "Longwatch requires a positive semi-automatic fire interval, range, and damage.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
