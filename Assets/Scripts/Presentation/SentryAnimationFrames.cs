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
        public const int DissolutionCount = 3;
        public const float WreckHoldDuration = 0.6f;
        public const float DissolutionFrameDuration = 0.13f;
        public const float DissolutionStart = DeathFrameDuration * 4f + WreckHoldDuration;
        public const float DeathEnd = DissolutionStart + DissolutionFrameDuration * DissolutionCount;

        // Indices 0..15 retain the approved sheet, 16..18 use a separate effect sheet;
        // -1 means renderer hidden. This clock is purely visual and never changes death state.
        public static int DeathFrame(float elapsed)
        {
            if (elapsed >= DeathEnd) return -1;
            if (elapsed >= DissolutionStart)
                return Count + Mathf.Clamp((int)((elapsed - DissolutionStart) / DissolutionFrameDuration), 0, 2);
            return 12 + Mathf.Clamp((int)(elapsed / DeathFrameDuration), 0, 3);
        }

        public static int Select(SentryState state, bool combatActive, bool moving, bool hitPaused,
            float remaining, float elapsed, float deathElapsed)
        {
            if (state == SentryState.Dead) return DeathFrame(deathElapsed);
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
