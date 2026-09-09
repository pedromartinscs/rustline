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
        private Vector2 _ledgeCaptureRootPosition;
        private Vector2 _ledgeFinalRootPosition;
        private float _ledgeClimbElapsed;

        public event Action Landed;
        public event Action<bool> Jumped;
        public event Action LedgeClimbStarted;

        public bool IsGrounded { get; private set; }
        public bool IsCrouched { get; private set; }
        public bool IsWallBraced { get; private set; }
        public bool IsLedgeClimbing { get; private set; }
        public bool IsWallKicking => WallKickLockRemaining > 0f;
        public int WallSide { get; private set; }
        public int LedgeSide { get; private set; }
        public float WallKickLockRemaining { get; private set; }
        public float LedgeClimbElapsed => _ledgeClimbElapsed;
        public int LedgeClimbFrameIndex => IsLedgeClimbing
            ? PlayerLedgeClimbMotion2D.GetFrameIndex(_ledgeClimbElapsed)
            : 0;
        public Vector2 LedgeCaptureRootPosition => _ledgeCaptureRootPosition;
        public Vector2 LedgeFinalRootPosition => _ledgeFinalRootPosition;
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
                if (IsLedgeClimbing)
                {
                    AdvanceLedgeClimb(deltaTime);
                    return;
                }

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

                bool facingLeft = _aim != null && _aim.FacingLeft;
                if (PlayerMovementMath.CanAttemptLedgeClimb(
                        IsGrounded,
                        velocity.y,
                        _input.MoveX,
                        facingLeft,
                        WallKickLockRemaining,
                        config))
                {
                    int ledgeSide = _input.MoveX < 0f ? -1 : 1;
                    if (_environmentProbe.TryFindLedgeClimb(
                            ledgeSide,
                            _body.position,
                            out LedgeClimbCandidate2D ledge))
                    {
                        BeginLedgeClimb(ledge);
                        return;
                    }
                }

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
                        facingLeft,
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
            CancelLedgeClimb();
            IsGrounded = false;
            if (_collider == null)
            {
                _collider = GetComponent<CapsuleCollider2D>();
            }
            if (_collider != null && config != null)
            {
                _collider.enabled = true;
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

        private void OnDisable()
        {
            CancelLedgeClimb();
        }

        private void BeginLedgeClimb(in LedgeClimbCandidate2D candidate)
        {
            SetCrouched(false);
            IsGrounded = false;
            IsWallBraced = false;
            WallSide = 0;
            LedgeSide = candidate.Side;
            _ledgeCaptureRootPosition = candidate.CaptureRootPosition;
            _ledgeFinalRootPosition = candidate.FinalRootPosition;
            _ledgeClimbElapsed = 0f;
            IsLedgeClimbing = true;
            WallKickLockRemaining = 0f;
            _jumpGrace.Reset();
            _input.ClearTransientState();
            _body.linearVelocity = Vector2.zero;
            _body.position = _ledgeCaptureRootPosition;
            _collider.enabled = false;
            LedgeClimbStarted?.Invoke();
        }

        private void AdvanceLedgeClimb(float deltaTime)
        {
            _jumpGrace.Reset();
            _input.ClearTransientState();
            _body.linearVelocity = Vector2.zero;
            _ledgeClimbElapsed = Mathf.Min(
                PlayerLedgeClimbMotion2D.TotalDuration,
                _ledgeClimbElapsed + deltaTime);

            if (_ledgeClimbElapsed < PlayerLedgeClimbMotion2D.TotalDuration)
            {
                _body.position = PlayerLedgeClimbMotion2D.GetPosition(
                    _ledgeCaptureRootPosition,
                    LedgeSide,
                    _ledgeClimbElapsed);
                return;
            }

            // Do not translate authored frame 4 after its hands reach the ledge. The final
            // standing placement happens only at the state boundary, when presentation also
            // leaves LedgeClimb for grounded locomotion.
            _body.position = _ledgeFinalRootPosition;
            _body.linearVelocity = Vector2.zero;
            _collider.enabled = true;
            _collider.size = config.StandingColliderSize;
            _collider.offset = config.StandingColliderOffset;
            IsCrouched = false;
            IsWallBraced = false;
            WallSide = 0;
            IsLedgeClimbing = false;
            LedgeSide = 0;
            IsGrounded = true;
            _jumpGrace.Reset();
            _input.ClearTransientState();
        }

        private void CancelLedgeClimb()
        {
            IsLedgeClimbing = false;
            LedgeSide = 0;
            _ledgeClimbElapsed = 0f;
            _ledgeCaptureRootPosition = Vector2.zero;
            _ledgeFinalRootPosition = Vector2.zero;
            if (_collider == null)
            {
                _collider = GetComponent<CapsuleCollider2D>();
            }

            if (_collider != null)
            {
                _collider.enabled = true;
                if (config != null)
                {
                    _collider.size = config.StandingColliderSize;
                    _collider.offset = config.StandingColliderOffset;
                }
            }
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
