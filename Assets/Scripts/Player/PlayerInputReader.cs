using UnityEngine;
using UnityEngine.InputSystem;

namespace Rustline.Gameplay.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string crouchActionName = "Crouch";
        [SerializeField] private string pointerPositionActionName = "PointerPosition";

        private InputActionMap _actionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _pointerPositionAction;
        private bool _jumpPressed;
        private bool _jumpReleased;

        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public Vector2 PointerScreenPosition { get; private set; }

        private void OnEnable()
        {
            ResolveActions();
            if (_actionMap == null)
            {
                return;
            }

            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _jumpAction.performed += OnJumpPerformed;
            _jumpAction.canceled += OnJumpCanceled;
            _crouchAction.performed += OnCrouch;
            _crouchAction.canceled += OnCrouch;
            _pointerPositionAction.performed += OnPointerPosition;
            _pointerPositionAction.canceled += OnPointerPosition;
            _actionMap.Enable();
            PointerScreenPosition = _pointerPositionAction.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            if (_actionMap != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMove;
                _jumpAction.performed -= OnJumpPerformed;
                _jumpAction.canceled -= OnJumpCanceled;
                _crouchAction.performed -= OnCrouch;
                _crouchAction.canceled -= OnCrouch;
                _pointerPositionAction.performed -= OnPointerPosition;
                _pointerPositionAction.canceled -= OnPointerPosition;
                _actionMap.Disable();
            }

            ClearTransientState();
            MoveX = 0f;
            JumpHeld = false;
            CrouchHeld = false;
        }

        public bool ConsumeJumpPressed()
        {
            bool value = _jumpPressed;
            _jumpPressed = false;
            return value;
        }

        public bool ConsumeJumpReleased()
        {
            bool value = _jumpReleased;
            _jumpReleased = false;
            return value;
        }

        public void ClearTransientState()
        {
            _jumpPressed = false;
            _jumpReleased = false;
        }

        private void ResolveActions()
        {
            _actionMap = inputActions != null ? inputActions.FindActionMap(actionMapName, false) : null;
            _moveAction = _actionMap?.FindAction(moveActionName, false);
            _jumpAction = _actionMap?.FindAction(jumpActionName, false);
            _crouchAction = _actionMap?.FindAction(crouchActionName, false);
            _pointerPositionAction = _actionMap?.FindAction(pointerPositionActionName, false);

            if (_actionMap == null || _moveAction == null || _jumpAction == null || _crouchAction == null ||
                _pointerPositionAction == null)
            {
                Debug.LogError(
                    "Rustline player input requires Player/Move, Player/Jump, Player/Crouch, and Player/PointerPosition actions.",
                    this);
                _actionMap = null;
            }
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            MoveX = Mathf.Clamp(context.ReadValue<Vector2>().x, -1f, 1f);
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!JumpHeld)
            {
                _jumpPressed = true;
            }

            JumpHeld = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            JumpHeld = false;
            _jumpReleased = true;
        }

        private void OnCrouch(InputAction.CallbackContext context)
        {
            CrouchHeld = context.ReadValueAsButton();
        }

        private void OnPointerPosition(InputAction.CallbackContext context)
        {
            PointerScreenPosition = context.ReadValue<Vector2>();
        }
    }
}
