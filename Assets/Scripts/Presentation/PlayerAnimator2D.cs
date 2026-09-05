using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Presentation
{
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerAnimator2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private PlayerAim2D playerAim;

        private PlayerMotor2D _motor;
        private float _landingTimeRemaining;
        private PlayerAnimationState? _currentState;

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

            bool facingFlipX = playerAim != null && playerAim.FacingFlipX;
            ApplyFacing(facingFlipX);

            PlayerAnimationState nextState = PlayerAnimationStateSelector.Select(
                _motor.IsGrounded,
                velocity.x,
                velocity.y,
                _landingTimeRemaining > 0f,
                facingFlipX,
                config.RunAnimationSpeedThreshold,
                config.AscendingAnimationThreshold);

            if (_currentState == nextState)
            {
                return;
            }

            animator.Play(nextState.ToString(), 0, 0f);
            _currentState = nextState;
        }

        private void OnLanded()
        {
            if (config != null)
            {
                _landingTimeRemaining = config.LandPresentationDuration;
            }
        }

        private void ApplyFacing(bool flipX)
        {
            bodySpriteRenderer.flipX = flipX;
            armsWeaponSpriteRenderer.flipX = flipX;
        }
    }
}
