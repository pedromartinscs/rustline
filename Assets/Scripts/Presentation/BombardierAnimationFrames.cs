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
        public const int DissolutionCount = 3;
        public const float WreckHoldDuration = 0.85f;
        public const float DissolutionFrameDuration = 0.13f;
        public const float DissolutionStart = DeathFrameDuration * 4f + WreckHoldDuration;
        public const float DeathEnd = DissolutionStart + DissolutionFrameDuration * DissolutionCount;

        public static int DeathFrame(float elapsed)
        {
            if (elapsed >= DeathEnd) return -1;
            if (elapsed >= DissolutionStart)
                return Count + Mathf.Clamp((int)((elapsed - DissolutionStart) / DissolutionFrameDuration), 0, 2);
            return 12 + Mathf.Clamp((int)(elapsed / DeathFrameDuration), 0, 3);
        }

        public static int Select(BombardierState state, bool active, bool moving, bool hitPaused,
            float remaining, float elapsed, float deathElapsed)
        {
            if (state == BombardierState.Dead) return DeathFrame(deathElapsed);
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
