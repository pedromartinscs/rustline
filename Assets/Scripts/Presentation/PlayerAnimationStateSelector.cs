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
        CrouchIdle,
        CrouchMove,
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
            return Select(
                grounded, horizontalVelocity, verticalVelocity, showingLanding, facingLeft,
                runThreshold, ascendingThreshold, false);
        }

        public static PlayerAnimationState Select(
            bool grounded,
            float horizontalVelocity,
            float verticalVelocity,
            bool showingLanding,
            bool facingLeft,
            float runThreshold,
            float ascendingThreshold,
            bool crouched)
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

            if (crouched)
            {
                return Mathf.Abs(horizontalVelocity) < runThreshold
                    ? PlayerAnimationState.CrouchIdle
                    : PlayerAnimationState.CrouchMove;
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
