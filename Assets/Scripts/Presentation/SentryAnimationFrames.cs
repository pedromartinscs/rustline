using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Frame selection only: combat timers/strike edges remain owned by SentryBrain2D.</summary>
    public static class SentryAnimationFrames
    {
        public const int Count = 16;
        public const float StrikeDisplayDuration = 0.12f;
        public const float DeathFrameDuration = 0.12f;

        public static int Select(SentryState state, bool combatActive, bool moving, bool hitPaused,
            float remaining, float elapsed, float deathElapsed)
        {
            if (state == SentryState.Dead) return 12 + Mathf.Clamp((int)(deathElapsed / DeathFrameDuration), 0, 3);
            if (hitPaused) return 11;
            if (!combatActive) return (int)(elapsed * 2f) % 2;
            switch (state)
            {
                case SentryState.Alert: return 6;
                case SentryState.Windup: return remaining > SentryBrain2D.WindupDuration * 0.5f ? 7 : 8;
                case SentryState.Recovery:
                    return remaining > SentryBrain2D.RecoveryDuration - StrikeDisplayDuration ? 9 : 10;
                default:
                    return moving ? 2 + (int)(elapsed * (state == SentryState.Pursuit ? 12f : 8f)) % 4 :
                        (int)(elapsed * 2f) % 2;
            }
        }
    }
}
