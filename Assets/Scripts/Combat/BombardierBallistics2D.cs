using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    public static class BombardierBallistics2D
    {
        public const float FlightTime = 0.85f;
        public const float GravityMagnitude = 22f;
        public const float CollisionRadius = 0.1875f;
        public const float ExplosionRadius = 1.75f;
        public const int ExplosionDamage = 25;
        public const float SafetyLifetime = 3f;

        public static Vector2 InitialVelocity(Vector2 origin, Vector2 target) =>
            (target - origin - 0.5f * new Vector2(0f, -GravityMagnitude) *
                (FlightTime * FlightTime)) / FlightTime;

        public static Vector2 Position(Vector2 origin, Vector2 initialVelocity, float time) =>
            origin + initialVelocity * time +
            0.5f * new Vector2(0f, -GravityMagnitude) * (time * time);

        public static bool IsInsideExplosion(Vector2 explosion, Vector2 nearestPlayerPoint) =>
            (nearestPlayerPoint - explosion).sqrMagnitude <= ExplosionRadius * ExplosionRadius;
    }
}
