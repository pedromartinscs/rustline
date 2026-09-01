using System;
using UnityEngine;

namespace Rustline.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader), typeof(PlayerGroundProbe2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;

        private Rigidbody2D _body;
        private PlayerInputReader _input;
        private PlayerGroundProbe2D _groundProbe;
        private PlayerJumpGrace _jumpGrace;

        public event Action Landed;

        public bool IsGrounded { get; private set; }
        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInputReader>();
            _groundProbe = GetComponent<PlayerGroundProbe2D>();
            _jumpGrace = new PlayerJumpGrace();
        }

        private void FixedUpdate()
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

            bool jumped = _jumpGrace.TryConsume();
            if (jumped)
            {
                velocity.y = config.JumpSpeed;
                IsGrounded = false;
            }

            if (_input.ConsumeJumpReleased() && velocity.y > 0f)
            {
                velocity.y = PlayerMovementMath.CutJumpVelocity(
                    velocity.y,
                    config);
            }

            velocity.x = PlayerMovementMath.CalculateHorizontalVelocity(
                velocity.x,
                _input.MoveX,
                IsGrounded,
                config,
                deltaTime);

            if (!jumped && !IsGrounded)
            {
                velocity.y = PlayerMovementMath.ApplyGravity(
                    velocity.y,
                    Physics2D.gravity.y,
                    config,
                    deltaTime);
            }

            _body.linearVelocity = velocity;
        }

        public void ResetAfterRespawn()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody2D>();
            }

            _body.linearVelocity = Vector2.zero;
            IsGrounded = false;
            _jumpGrace ??= new PlayerJumpGrace();
            _jumpGrace.Reset();
            _input?.ClearTransientState();
        }
    }
}
