using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    public static class BombardierExplosion2D
    {
        private const float SurfaceOffset = 1f / 1024f;

        public static bool TryDamagePlayer(Vector2 point, Vector2 impactNormal,
            Collider2D directCollider, CombatHealth2D playerHealth,
            CapsuleCollider2D playerCollider, Object source)
        {
            if (playerHealth == null || !playerHealth.IsAlive ||
                playerCollider == null || !playerCollider.enabled) return false;
            Vector2 nearestPlayerPoint = playerCollider.ClosestPoint(point);
            if (!BombardierBallistics2D.IsInsideExplosion(point, nearestPlayerPoint)) return false;
            Vector2 target = playerCollider.bounds.center;
            if (directCollider != playerCollider)
            {
                Vector2 rayOrigin = point + impactNormal * SurfaceOffset;
                Vector2 delta = target - rayOrigin;
                if (Physics2D.Raycast(rayOrigin, delta.normalized, delta.magnitude, 1 << 6).collider != null)
                    return false;
            }
            var damage = new DamageInfo2D(BombardierBallistics2D.ExplosionDamage,
                point, (target - point).normalized, source);
            return playerHealth.ApplyDamage(in damage).DidApply;
        }
    }
}
