using System;

namespace Rustline.Gameplay.Combat
{
    public enum SentryState { Patrol, Alert, Pursuit, Windup, Recovery, Dead }

    /// <summary>Clocked combat policy. Geometry and damage are supplied by the actor.</summary>
    public sealed class SentryBrain2D
    {
        public const float AlertDuration = 0.35f;
        public const float WindupDuration = 0.48f;
        public const float RecoveryDuration = 0.9f;
        public SentryState State { get; private set; }
        public float Remaining { get; private set; }

        // True only on the single committed strike edge; callers recheck geometry then.
        public bool Step(float deltaTime, bool seesPlayer, bool inReach)
        {
            if (deltaTime < 0f || !float.IsFinite(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            switch (State)
            {
                case SentryState.Patrol:
                    if (seesPlayer) Enter(SentryState.Alert, AlertDuration);
                    break;
                case SentryState.Alert:
                    Remaining = Math.Max(0f, Remaining - deltaTime);
                    if (Remaining <= 0f) Enter(seesPlayer ? SentryState.Pursuit : SentryState.Patrol);
                    break;
                case SentryState.Pursuit:
                    if (!seesPlayer) Enter(SentryState.Patrol);
                    else if (inReach) Enter(SentryState.Windup, WindupDuration);
                    break;
                case SentryState.Windup:
                    Remaining = Math.Max(0f, Remaining - deltaTime);
                    if (Remaining <= 0f)
                    {
                        Enter(SentryState.Recovery, RecoveryDuration);
                        return true;
                    }
                    break;
                case SentryState.Recovery:
                    Remaining = Math.Max(0f, Remaining - deltaTime);
                    if (Remaining <= 0f) Enter(seesPlayer ? SentryState.Pursuit : SentryState.Patrol);
                    break;
            }
            return false;
        }

        public void Kill() => Enter(SentryState.Dead);
        public void Reset() => Enter(SentryState.Patrol);
        private void Enter(SentryState state, float duration = 0f)
        {
            State = state;
            Remaining = duration;
        }
    }
}
