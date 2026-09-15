using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    public interface IWeaponHitReceiver2D
    {
        void ReceiveHit(in WeaponHitInfo2D hit);
    }

    /// <summary>
    /// Marks a hit receiver as a combat target that projectile weapons may damage and stop on.
    /// Environmental receivers can still implement IWeaponHitReceiver2D without becoming combat targets.
    /// </summary>
    public interface IWeaponCombatTarget2D : IWeaponHitReceiver2D
    {
    }

    public readonly struct WeaponShotFired2D
    {
        public WeaponShotFired2D(
            WeaponDefinition2D weapon,
            WeaponShotMode2D shotMode,
            Vector2 origin,
            Vector2 direction)
        {
            Weapon = weapon;
            ShotMode = shotMode;
            Origin = origin;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        public WeaponDefinition2D Weapon { get; }
        public string WeaponId => Weapon != null ? Weapon.WeaponId : string.Empty;
        public WeaponShotMode2D ShotMode { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
    }

    public readonly struct WeaponHitInfo2D
    {
        public WeaponHitInfo2D(
            WeaponDefinition2D weapon,
            Vector2 origin,
            Vector2 direction,
            Vector2 point,
            Vector2 normal,
            float distance,
            int damage,
            Collider2D collider)
        {
            Weapon = weapon;
            Origin = origin;
            Direction = direction;
            Point = point;
            Normal = normal;
            Distance = distance;
            Damage = damage;
            Collider = collider;
        }

        public WeaponDefinition2D Weapon { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public float Distance { get; }
        public int Damage { get; }
        public Collider2D Collider { get; }
    }

    public readonly struct WeaponShotResult2D
    {
        public WeaponShotResult2D(
            WeaponDefinition2D weapon,
            Vector2 origin,
            Vector2 direction,
            Vector2 endPoint,
            bool hit,
            Collider2D hitCollider,
            Vector2 hitNormal,
            float hitDistance,
            int damage,
            bool hitReceiverNotified)
            : this(
                weapon,
                WeaponShotMode2D.Conventional,
                origin,
                direction,
                direction,
                endPoint,
                hit,
                hitCollider,
                hitNormal,
                hitDistance,
                damage,
                hitReceiverNotified,
                0)
        {
        }

        public WeaponShotResult2D(
            WeaponDefinition2D weapon,
            WeaponShotMode2D shotMode,
            Vector2 origin,
            Vector2 direction,
            Vector2 finalDirection,
            Vector2 endPoint,
            bool hit,
            Collider2D hitCollider,
            Vector2 hitNormal,
            float hitDistance,
            int damage,
            bool hitReceiverNotified,
            int bounceCount)
        {
            Weapon = weapon;
            ShotMode = shotMode;
            Origin = origin;
            Direction = direction;
            FinalDirection = finalDirection;
            EndPoint = endPoint;
            Hit = hit;
            HitCollider = hitCollider;
            HitNormal = hitNormal;
            HitDistance = hitDistance;
            Damage = damage;
            HitReceiverNotified = hitReceiverNotified;
            BounceCount = bounceCount;
        }

        public WeaponDefinition2D Weapon { get; }
        public string WeaponId => Weapon != null ? Weapon.WeaponId : string.Empty;
        public WeaponShotMode2D ShotMode { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public Vector2 FinalDirection { get; }
        public Vector2 EndPoint { get; }
        public bool Hit { get; }
        public Collider2D HitCollider { get; }
        public Vector2 HitNormal { get; }
        public float HitDistance { get; }
        public int Damage { get; }
        public bool HitReceiverNotified { get; }
        public int BounceCount { get; }
    }

    public static class WeaponFirePolicy2D
    {
        public static bool CanFire(
            PlayerAnimationState? state,
            bool isWallBraced,
            bool isWallKicking)
        {
            if (isWallBraced || isWallKicking)
            {
                return false;
            }

            return state == PlayerAnimationState.Idle ||
                   state == PlayerAnimationState.Run ||
                   state == PlayerAnimationState.Backpedal ||
                   state == PlayerAnimationState.CrouchIdle ||
                   state == PlayerAnimationState.CrouchMove ||
                   state == PlayerAnimationState.Fall;
        }
    }

    public static class WeaponBounceMath2D
    {
        /// <summary>
        /// Returns the specular reflection of a travel direction around a surface normal.
        /// For normalized vectors the reflection is r = d - 2 * dot(d, n) * n:
        /// the tangential component is preserved and the normal component is inverted.
        /// </summary>
        public static Vector2 Reflect(Vector2 direction, Vector2 normal)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            Vector2 normalizedNormal = normal.sqrMagnitude > 0f
                ? normal.normalized
                : -normalizedDirection;

            float normalProjection = Vector2.Dot(normalizedDirection, normalizedNormal);
            Vector2 reflected = normalizedDirection -
                                2f * normalProjection * normalizedNormal;
            return reflected.sqrMagnitude > 0f ? reflected.normalized : -normalizedDirection;
        }

        /// <summary>
        /// bounceCount is the number of reflections already completed. With maxBounces = 3,
        /// collisions at counts 0, 1 and 2 may reflect; once count reaches 3 the next surface ends it.
        /// </summary>
        public static bool CanReflect(
            WeaponShotMode2D shotMode,
            int bounceCount,
            int maxBounces)
        {
            return shotMode == WeaponShotMode2D.Bouncing &&
                   bounceCount >= 0 &&
                   bounceCount < Mathf.Max(0, maxBounces);
        }
    }

    public sealed class SemiAutomaticWeaponCooldown2D
    {
        private bool _hasFired;
        private float _readyTime;

        public float ReadyTime => _readyTime;

        public bool TryConsume(float currentTime, float shotInterval)
        {
            if (_hasFired && currentTime < _readyTime)
            {
                return false;
            }

            _hasFired = true;
            _readyTime = currentTime + shotInterval;
            return true;
        }

        public void Reset()
        {
            _hasFired = false;
            _readyTime = 0f;
        }
    }
}
