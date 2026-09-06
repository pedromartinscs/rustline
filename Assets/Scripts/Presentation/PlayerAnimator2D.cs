using Rustline.Gameplay.Player;
using Unity.Profiling;
using UnityEngine;

namespace Rustline.Presentation
{
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerAnimator2D : MonoBehaviour
    {
        private static readonly ProfilerMarker AnimatorMarker =
            new ProfilerMarker("Rustline.Presentation.Animator");
        private static readonly int IdleStateHash = Animator.StringToHash(nameof(PlayerAnimationState.Idle));
        private static readonly int RunStateHash = Animator.StringToHash(nameof(PlayerAnimationState.Run));
        private static readonly int BackpedalStateHash = Animator.StringToHash(nameof(PlayerAnimationState.Backpedal));
        private static readonly int JumpStateHash = Animator.StringToHash(nameof(PlayerAnimationState.Jump));
        private static readonly int FallStateHash = Animator.StringToHash(nameof(PlayerAnimationState.Fall));
        private static readonly int LandStateHash = Animator.StringToHash(nameof(PlayerAnimationState.Land));
        private static readonly int CrouchIdleStateHash = Animator.StringToHash(nameof(PlayerAnimationState.CrouchIdle));
        private static readonly int CrouchMoveStateHash = Animator.StringToHash(nameof(PlayerAnimationState.CrouchMove));

        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private PlayerAim2D playerAim;

        private PlayerMotor2D _motor;
        private float _landingTimeRemaining;
        private PlayerAnimationState? _currentState;
        private bool? _lastFacingLeft;

        public PlayerAnimationState? CurrentState => _currentState;
        public PlayerAim2D PlayerAim => playerAim;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor2D>();
        }

        private void OnEnable()
        {
            if (_motor == null)
            {
                _motor = GetComponent<PlayerMotor2D>();
            }

            _motor.Landed += OnLanded;
            _lastFacingLeft = null;
        }

        private void OnDisable()
        {
            if (_motor != null)
            {
                _motor.Landed -= OnLanded;
            }
        }

        private void Update()
        {
            using (AnimatorMarker.Auto())
            {
                if (config == null || animator == null || bodySpriteRenderer == null || armsWeaponSpriteRenderer == null)
                {
                    return;
                }

                Vector2 velocity = _motor.Velocity;
                if (_motor.IsGrounded)
                {
                    _landingTimeRemaining = Mathf.Max(0f, _landingTimeRemaining - Time.deltaTime);
                }
                else
                {
                    _landingTimeRemaining = 0f;
                }

                bool facingLeft = playerAim != null && playerAim.FacingLeft;
                ApplyFacing(facingLeft);

                PlayerAnimationState nextState = PlayerAnimationStateSelector.Select(
                    _motor.IsGrounded,
                    velocity.x,
                    velocity.y,
                    _landingTimeRemaining > 0f,
                    facingLeft,
                    config.RunAnimationSpeedThreshold,
                    config.AscendingAnimationThreshold,
                    _motor.IsCrouched);

                if (_currentState == nextState)
                {
                    return;
                }

                animator.Play(GetStateHash(nextState), 0, 0f);
                _currentState = nextState;
            }
        }

        private void OnLanded()
        {
            if (config != null)
            {
                _landingTimeRemaining = config.LandPresentationDuration;
            }
        }

        private void ApplyFacing(bool facingLeft)
        {
            if (_lastFacingLeft == facingLeft)
            {
                return;
            }

            bodySpriteRenderer.flipX = facingLeft;
            armsWeaponSpriteRenderer.flipX = facingLeft;
            _lastFacingLeft = facingLeft;
        }

        private static int GetStateHash(PlayerAnimationState state)
        {
            switch (state)
            {
                case PlayerAnimationState.Idle: return IdleStateHash;
                case PlayerAnimationState.Run: return RunStateHash;
                case PlayerAnimationState.Backpedal: return BackpedalStateHash;
                case PlayerAnimationState.Jump: return JumpStateHash;
                case PlayerAnimationState.Fall: return FallStateHash;
                case PlayerAnimationState.Land: return LandStateHash;
                case PlayerAnimationState.CrouchIdle: return CrouchIdleStateHash;
                case PlayerAnimationState.CrouchMove: return CrouchMoveStateHash;
                default: return IdleStateHash;
            }
        }
    }
}
