using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Visual selection from authoritative combat state; never launches a projectile.</summary>
    public static class BombardierAnimationFrames
    {
        public const int Count = 16;
        public const float LaunchDisplayDuration = 0.12f;
        public const float DeathFrameDuration = 0.14f;

        public static int Select(BombardierState state, bool active, bool moving, bool hitPaused,
            float remaining, float elapsed, float deathElapsed)
        {
            if (state == BombardierState.Dead) return 12 + Mathf.Clamp((int)(deathElapsed / DeathFrameDuration), 0, 3);
            if (hitPaused) return 11;
            if (!active) return (int)(elapsed * 2f) % 2;
            switch (state)
            {
                case BombardierState.Windup:
                    return remaining > Bombardier2D.WindupDuration * 2f / 3f ? 6 :
                        remaining > Bombardier2D.WindupDuration / 3f ? 7 : 8;
                case BombardierState.Recover:
                    return remaining > Bombardier2D.RecoveryDuration - LaunchDisplayDuration ? 9 : 10;
                default: return moving ? 2 + (int)(elapsed * 8f) % 4 : (int)(elapsed * 2f) % 2;
            }
        }
    }
}
