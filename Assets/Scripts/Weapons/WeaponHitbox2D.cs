using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class WeaponHitbox2D : MonoBehaviour, IWeaponHitReceiver2D
    {
        [SerializeField] private CombatHealth2D health;

        public CombatHealth2D Health => health;
        public int ReceivedHitCount { get; private set; }
        public DamageResult2D LastDamageResult { get; private set; }

        public void Configure(CombatHealth2D owningHealth)
        {
            health = owningHealth;
        }

        public void ReceiveHit(in WeaponHitInfo2D hit)
        {
            ReceivedHitCount++;
            if (health == null)
            {
                LastDamageResult = default;
                return;
            }

            var damage = new DamageInfo2D(
                hit.Damage,
                hit.Point,
                hit.Direction,
                hit.Weapon);
            LastDamageResult = health.ApplyDamage(in damage);
        }
    }
}
