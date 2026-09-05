using Mathf = UnityEngine.Mathf;

namespace Rustline.Presentation
{
    public enum PlayerAnimationState
    {
        Idle,
        Run,
        Backpedal,
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
            bool facingLeft,
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

            if (Mathf.Abs(horizontalVelocity) < runThreshold)
            {
                return PlayerAnimationState.Idle;
            }

            return Gameplay.Player.PlayerMovementMath.IsDirectionAgainstFacing(
                horizontalVelocity,
                facingLeft)
                ? PlayerAnimationState.Backpedal
                : PlayerAnimationState.Run;
        }
    }
}
