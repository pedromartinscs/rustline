using System;
using Unity.Profiling;
using UnityEngine;

namespace Rustline.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader), typeof(PlayerGroundProbe2D))]
    [RequireComponent(typeof(CapsuleCollider2D), typeof(PlayerEnvironmentProbe2D))]
    [RequireComponent(typeof(PlayerAim2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        private static readonly ProfilerMarker MotorMarker = new ProfilerMarker("Rustline.Player.Motor");

        [SerializeField] private PlayerMovementConfig config;

        private Rigidbody2D _body;
        private PlayerInputReader _input;
        private PlayerGroundProbe2D _groundProbe;
        private PlayerEnvironmentProbe2D _environmentProbe;
        private CapsuleCollider2D _collider;
        private PlayerAim2D _aim;
        private PlayerJumpGrace _jumpGrace;

        public event Action Landed;
        public event Action<bool> Jumped;

        public bool IsGrounded { get; private set; }
        public bool IsCrouched { get; private set; }
        public bool IsWallBraced { get; private set; }
        public bool IsWallKicking => WallKickLockRemaining > 0f;
        public int WallSide { get; private set; }
        public float WallKickLockRemaining { get; private set; }
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInputReader>();
            _groundProbe = GetComponent<PlayerGroundProbe2D>();
            _environmentProbe = GetComponent<PlayerEnvironmentProbe2D>();
            _collider = GetComponent<CapsuleCollider2D>();
            _aim = GetComponent<PlayerAim2D>();
            _jumpGrace = new PlayerJumpGrace();
        }

        private void FixedUpdate()
        {
            using (MotorMarker.Auto())
            {
                if (config == null)
                {
                    return;
                }

                float deltaTime = Time.fixedDeltaTime;
                Vector2 velocity = _body.linearVelocity;
                bool wasGrounded = IsGrounded;
                IsGrounded = _groundProbe.CheckGrounded(velocity.y);

                if (!wasGrounded && IsGrounded)
                {
                    Landed?.Invoke();
                }

                if (_input.ConsumeJumpPressed())
                {
                    _jumpGrace.Buffer(config.JumpBufferTime);
                }

                _jumpGrace.Tick(IsGrounded, deltaTime, config.CoyoteTime);

                WallKickLockRemaining = Mathf.Max(0f, WallKickLockRemaining - deltaTime);
                bool hasStandingClearance = UpdateCrouchPosture(_jumpGrace.HasBufferedJump);

                int contactedWallSide = PlayerMovementMath.CanAttemptWallBrace(
                    IsGrounded,
                    velocity.y,
                    _input.MoveX,
                    config)
                    ? _environmentProbe.FindWallSide(_input.MoveX)
                    : 0;
                IsWallBraced = PlayerMovementMath.CanWallBrace(
                    IsGrounded,
                    velocity.y,
                    _input.MoveX,
                    contactedWallSide,
                    WallSide,
                    WallKickLockRemaining,
                    config);
                if (IsWallBraced)
                {
                    WallSide = contactedWallSide;
                }
                else if (WallKickLockRemaining <= 0f)
                {
                    WallSide = 0;
                }

                bool wallKicked = IsWallBraced && _jumpGrace.TryConsumeBuffered();
                bool canUseGroundJump = !IsCrouched || hasStandingClearance;
                bool jumped = !wallKicked && canUseGroundJump && _jumpGrace.TryConsume();
                if (wallKicked)
                {
                    int kickedWallSide = WallSide;
                    velocity = PlayerMovementMath.GetWallKickVelocity(kickedWallSide, config);
                    WallKickLockRemaining = config.WallKickLockDuration;
                    IsWallBraced = false;
                    WallSide = kickedWallSide;
                }
                if (jumped)
                {
                    SetCrouched(false);
                    bool jumpedWhileGrounded = IsGrounded;
                    velocity.y = config.JumpSpeed;
                    IsGrounded = false;
                    Jumped?.Invoke(jumpedWhileGrounded);
                }

                if (_input.ConsumeJumpReleased() && velocity.y > 0f)
                {
                    velocity.y = PlayerMovementMath.CutJumpVelocity(
                        velocity.y,
                        config);
                }

                if (WallKickLockRemaining <= 0f)
                {
                    velocity.x = PlayerMovementMath.CalculateHorizontalVelocity(
                        velocity.x,
                        _input.MoveX,
                        IsGrounded,
                        _aim != null && _aim.FacingLeft,
                        IsCrouched,
                        config,
                        deltaTime);
                }

                if (!jumped && !wallKicked && !IsGrounded)
                {
                    velocity.y = PlayerMovementMath.ApplyGravity(
                        velocity.y,
                        Physics2D.gravity.y,
                        config,
                        deltaTime);
                }

                if (IsWallBraced)
                {
                    velocity.y = PlayerMovementMath.CapWallBraceFallVelocity(velocity.y, config);
                }

                _body.linearVelocity = velocity;
            }
        }

        public void ResetAfterRespawn()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody2D>();
            }

            _body.linearVelocity = Vector2.zero;
            IsGrounded = false;
            if (_collider == null)
            {
                _collider = GetComponent<CapsuleCollider2D>();
            }
            if (_collider != null && config != null)
            {
                _collider.size = config.StandingColliderSize;
                _collider.offset = config.StandingColliderOffset;
            }
            IsCrouched = false;
            IsWallBraced = false;
            WallSide = 0;
            WallKickLockRemaining = 0f;
            _jumpGrace ??= new PlayerJumpGrace();
            _jumpGrace.Reset();
            _input?.ClearTransientState();
        }

        private bool UpdateCrouchPosture(bool hasBufferedJump)
        {
            if (IsGrounded && _input.CrouchHeld)
            {
                SetCrouched(true);
            }

            bool shouldAttemptStand = IsCrouched && (!IsGrounded || !_input.CrouchHeld);
            bool needsStandingClearance = shouldAttemptStand || IsCrouched && hasBufferedJump;
            if (!needsStandingClearance)
            {
                return false;
            }

            bool hasStandingClearance = _environmentProbe.HasStandingClearance();
            if (shouldAttemptStand && hasStandingClearance)
            {
                SetCrouched(false);
            }

            return hasStandingClearance;
        }

        private void SetCrouched(bool crouched)
        {
            if (_collider == null || config == null || IsCrouched == crouched)
            {
                return;
            }

            _collider.size = crouched ? config.CrouchColliderSize : config.StandingColliderSize;
            _collider.offset = crouched ? config.CrouchColliderOffset : config.StandingColliderOffset;
            IsCrouched = crouched;
        }
    }
}
