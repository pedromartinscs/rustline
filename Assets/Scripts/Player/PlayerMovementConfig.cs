using UnityEngine;

namespace Rustline.Gameplay.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Rustline/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Horizontal movement")]
        [SerializeField, Min(0.01f)] private float maxGroundSpeed = 7f;
        [SerializeField, Min(0.01f)] private float maxBackpedalGroundSpeed = 4f;
        [SerializeField, Min(0.01f)] private float groundAcceleration = 55f;
        [SerializeField, Min(0.01f)] private float groundDeceleration = 70f;
        [SerializeField, Min(0.01f)] private float directionChangeAcceleration = 90f;
        [SerializeField, Min(0.01f)] private float maxAirSpeed = 7f;
        [SerializeField, Min(0.01f)] private float airAcceleration = 30f;
        [SerializeField, Range(0f, 0.5f)] private float inputDeadZone = 0.1f;

        [Header("Jump")]
        [SerializeField, Min(0.01f)] private float jumpSpeed = 12.5f;
        [SerializeField, Range(0.05f, 1f)] private float jumpReleaseVelocityMultiplier = 0.45f;
        [SerializeField, Range(0f, 0.5f)] private float coyoteTime = 0.12f;
        [SerializeField, Range(0f, 0.5f)] private float jumpBufferTime = 0.12f;

        [Header("Gravity")]
        [SerializeField, Min(0.01f)] private float ascentGravityScale = 3f;
        [SerializeField, Min(0.01f)] private float fallGravityScale = 4.5f;
        [SerializeField, Min(0.01f)] private float maxFallSpeed = 18f;

        [Header("Ground detection")]
        [SerializeField, Range(0.001f, 0.25f)] private float groundCheckDistance = 0.075f;
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.65f;
        [SerializeField, Min(0f)] private float maximumGroundingUpwardSpeed = 0.1f;

        [Header("Presentation")]
        [SerializeField, Min(0f)] private float runAnimationSpeedThreshold = 0.2f;
        [SerializeField, Min(0f)] private float ascendingAnimationThreshold = 0.15f;
        [SerializeField, Min(0f)] private float facingVelocityThreshold = 0.05f;
        [SerializeField, Min(0f)] private float landPresentationDuration = 0.22f;

        public float MaxGroundSpeed => maxGroundSpeed;
        public float MaxBackpedalGroundSpeed => maxBackpedalGroundSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float DirectionChangeAcceleration => directionChangeAcceleration;
        public float MaxAirSpeed => maxAirSpeed;
        public float AirAcceleration => airAcceleration;
        public float InputDeadZone => inputDeadZone;
        public float JumpSpeed => jumpSpeed;
        public float JumpReleaseVelocityMultiplier => jumpReleaseVelocityMultiplier;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float AscentGravityScale => ascentGravityScale;
        public float FallGravityScale => fallGravityScale;
        public float MaxFallSpeed => maxFallSpeed;
        public float GroundCheckDistance => groundCheckDistance;
        public float MinimumGroundNormalY => minimumGroundNormalY;
        public float MaximumGroundingUpwardSpeed => maximumGroundingUpwardSpeed;
        public float RunAnimationSpeedThreshold => runAnimationSpeedThreshold;
        public float AscendingAnimationThreshold => ascendingAnimationThreshold;
        public float FacingVelocityThreshold => facingVelocityThreshold;
        public float LandPresentationDuration => landPresentationDuration;

        public bool IsSane(out string reason)
        {
            if (maxGroundSpeed <= 0f || maxBackpedalGroundSpeed <= 0f ||
                maxBackpedalGroundSpeed > maxGroundSpeed || maxAirSpeed <= 0f)
            {
                reason = "Maximum movement speeds must be positive and Backpedal must not exceed forward ground speed.";
                return false;
            }

            if (groundAcceleration <= 0f || groundDeceleration <= 0f ||
                directionChangeAcceleration <= 0f || airAcceleration <= 0f)
            {
                reason = "Movement accelerations must be positive.";
                return false;
            }

            if (jumpSpeed <= 0f || jumpReleaseVelocityMultiplier <= 0f || jumpReleaseVelocityMultiplier > 1f)
            {
                reason = "Jump speed and jump-release multiplier are invalid.";
                return false;
            }

            if (coyoteTime < 0f || jumpBufferTime < 0f || ascentGravityScale <= 0f ||
                fallGravityScale <= 0f || maxFallSpeed <= 0f)
            {
                reason = "Grace windows and gravity values are invalid.";
                return false;
            }

            if (groundCheckDistance <= 0f || minimumGroundNormalY < 0f || minimumGroundNormalY > 1f)
            {
                reason = "Ground-check values are invalid.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
