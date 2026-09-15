using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    public interface IWeaponHitReceiver2D
    {
        void ReceiveHit(in WeaponHitInfo2D hit);
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
        public static Vector2 Reflect(Vector2 direction, Vector2 normal)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;
            Vector2 normalizedNormal = normal.sqrMagnitude > 0f
                ? normal.normalized
                : -normalizedDirection;
            Vector2 reflected = Vector2.Reflect(normalizedDirection, normalizedNormal);
            return reflected.sqrMagnitude > 0f ? reflected.normalized : -normalizedDirection;
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
