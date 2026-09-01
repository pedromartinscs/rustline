using Mathf = UnityEngine.Mathf;

namespace Rustline.Presentation
{
    public enum PlayerAnimationState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Land,
    }

    public static class PlayerAnimationStateSelector
    {
        public static PlayerAnimationState Select(
            bool grounded,
            float horizontalVelocity,
            float verticalVelocity,
            bool showingLanding,
            float runThreshold,
            float ascendingThreshold)
        {
            if (!grounded)
            {
                return verticalVelocity > ascendingThreshold
                    ? PlayerAnimationState.Jump
                    : PlayerAnimationState.Fall;
            }

            if (showingLanding)
            {
                return PlayerAnimationState.Land;
            }

            return Mathf.Abs(horizontalVelocity) >= runThreshold
                ? PlayerAnimationState.Run
                : PlayerAnimationState.Idle;
        }
    }
}
