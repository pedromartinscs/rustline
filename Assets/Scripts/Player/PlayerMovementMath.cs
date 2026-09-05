using UnityEngine;

namespace Rustline.Gameplay.Player
{
    public static class PlayerMovementMath
    {
        public static float CalculateHorizontalVelocity(
            float currentVelocity,
            float rawInput,
            bool grounded,
            bool facingLeft,
            PlayerMovementConfig config,
            float deltaTime)
        {
            float input = Mathf.Abs(rawInput) < config.InputDeadZone
                ? 0f
                : Mathf.Clamp(rawInput, -1f, 1f);
            float maxSpeed = grounded
                ? GetGroundSpeedLimit(input, facingLeft, config)
                : config.MaxAirSpeed;
            float targetVelocity = input * maxSpeed;
            float acceleration;

            if (!grounded)
            {
                acceleration = input == 0f ? 0f : config.AirAcceleration;
            }
            else if (input == 0f)
            {
                acceleration = config.GroundDeceleration;
            }
            else if (Mathf.Abs(currentVelocity) > 0.01f && Mathf.Sign(currentVelocity) != Mathf.Sign(targetVelocity))
            {
                acceleration = config.DirectionChangeAcceleration;
            }
            else
            {
                acceleration = config.GroundAcceleration;
            }

            return Mathf.MoveTowards(currentVelocity, targetVelocity, acceleration * deltaTime);
        }

        public static float GetGroundSpeedLimit(
            float horizontalDirection,
            bool facingLeft,
            PlayerMovementConfig config)
        {
            return IsDirectionAgainstFacing(horizontalDirection, facingLeft)
                ? config.MaxBackpedalGroundSpeed
                : config.MaxGroundSpeed;
        }

        public static bool IsDirectionAgainstFacing(float horizontalDirection, bool facingLeft)
        {
            return horizontalDirection < 0f && !facingLeft ||
                   horizontalDirection > 0f && facingLeft;
        }

        public static float ApplyGravity(
            float verticalVelocity,
            float gravityY,
            PlayerMovementConfig config,
            float deltaTime)
        {
            float gravityScale = verticalVelocity > 0f
                ? config.AscentGravityScale
                : config.FallGravityScale;
            float nextVelocity = verticalVelocity + gravityY * gravityScale * deltaTime;
            return Mathf.Max(nextVelocity, -config.MaxFallSpeed);
        }

        public static float CutJumpVelocity(float verticalVelocity, PlayerMovementConfig config)
        {
            return verticalVelocity > 0f
                ? verticalVelocity * config.JumpReleaseVelocityMultiplier
                : verticalVelocity;
        }
    }
}
