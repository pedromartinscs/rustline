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
        [SerializeField] private string fireActionName = "Fire";
        [SerializeField] private string reloadActionName = "Reload";
        [SerializeField] private string toggleFireModeActionName = "ToggleFireMode";
        [SerializeField] private string pointerPositionActionName = "PointerPosition";
        [SerializeField] private string weaponCycleActionName = "WeaponCycle";
        [SerializeField] private string weaponSlotActionPrefix = "WeaponSlot";

        private InputActionMap _actionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private InputAction _toggleFireModeAction;
        private InputAction _pointerPositionAction;
        private InputAction _weaponCycleAction;
        private readonly InputAction[] _weaponSlotActions = new InputAction[10];
        private bool _jumpPressed;
        private bool _jumpReleased;
        private bool _firePressed;
        private bool _reloadPressed;
        private bool _fireHeld;
        private bool _toggleFireModePressed;
        private int _weaponCycleDirection;
        private int _weaponSlotPressed = -1;

        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool IsFireHeld => _fireHeld;
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
            _fireAction.performed += OnFirePerformed;
            _fireAction.canceled += OnFireCanceled;
            _reloadAction.performed += OnReloadPerformed;
            _toggleFireModeAction.performed += OnToggleFireModePerformed;
            _pointerPositionAction.performed += OnPointerPosition;
            _pointerPositionAction.canceled += OnPointerPosition;
            _weaponCycleAction.performed += OnWeaponCycle;
            for (int slot = 0; slot < _weaponSlotActions.Length; slot++)
            {
                _weaponSlotActions[slot].performed += OnWeaponSlot;
            }
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
                _fireAction.performed -= OnFirePerformed;
                _fireAction.canceled -= OnFireCanceled;
                _reloadAction.performed -= OnReloadPerformed;
                _toggleFireModeAction.performed -= OnToggleFireModePerformed;
                _pointerPositionAction.performed -= OnPointerPosition;
                _pointerPositionAction.canceled -= OnPointerPosition;
                _weaponCycleAction.performed -= OnWeaponCycle;
                for (int slot = 0; slot < _weaponSlotActions.Length; slot++)
                {
                    _weaponSlotActions[slot].performed -= OnWeaponSlot;
                }
                _actionMap.Disable();
            }

            ClearTransientState();
            MoveX = 0f;
            JumpHeld = false;
            CrouchHeld = false;
            _fireHeld = false;
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

        public bool ConsumeFirePressed()
        {
            bool value = _firePressed;
            _firePressed = false;
            return value;
        }

        public bool ConsumeToggleFireModePressed()
        {
            bool value = _toggleFireModePressed;
            _toggleFireModePressed = false;
            return value;
        }

        public bool ConsumeReloadPressed()
        {
            bool value = _reloadPressed;
            _reloadPressed = false;
            return value;
        }

        public int ConsumeWeaponCycleDirection()
        {
            int value = _weaponCycleDirection;
            _weaponCycleDirection = 0;
            return value;
        }

        public int ConsumeWeaponSlotPressed()
        {
            int value = _weaponSlotPressed;
            _weaponSlotPressed = -1;
            return value;
        }

        public void ClearTransientState()
        {
            _jumpPressed = false;
            _jumpReleased = false;
            _firePressed = false;
            _reloadPressed = false;
            _toggleFireModePressed = false;
            _weaponCycleDirection = 0;
            _weaponSlotPressed = -1;
        }

        private void ResolveActions()
        {
            _actionMap = inputActions != null ? inputActions.FindActionMap(actionMapName, false) : null;
            _moveAction = _actionMap?.FindAction(moveActionName, false);
            _jumpAction = _actionMap?.FindAction(jumpActionName, false);
            _crouchAction = _actionMap?.FindAction(crouchActionName, false);
            _fireAction = _actionMap?.FindAction(fireActionName, false);
            _reloadAction = _actionMap?.FindAction(reloadActionName, false);
            _toggleFireModeAction = _actionMap?.FindAction(toggleFireModeActionName, false);
            _pointerPositionAction = _actionMap?.FindAction(pointerPositionActionName, false);
            _weaponCycleAction = _actionMap?.FindAction(weaponCycleActionName, false);
            for (int slot = 0; slot < _weaponSlotActions.Length; slot++)
            {
                _weaponSlotActions[slot] = _actionMap?.FindAction(weaponSlotActionPrefix + slot, false);
            }

            if (_actionMap == null || _moveAction == null || _jumpAction == null || _crouchAction == null ||
                _fireAction == null || _reloadAction == null || _toggleFireModeAction == null || _pointerPositionAction == null ||
                _weaponCycleAction == null)
            {
                Debug.LogError(
                    "Rustline player input requires its movement, fire, pointer, and weapon-selection actions.",
                    this);
                _actionMap = null;
                return;
            }

            for (int slot = 0; slot < _weaponSlotActions.Length; slot++)
            {
                if (_weaponSlotActions[slot] == null)
                {
                    Debug.LogError("Rustline player input requires Player/WeaponSlot0 through Player/WeaponSlot9.", this);
                    _actionMap = null;
                    return;
                }
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

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            if (!_fireHeld)
            {
                _firePressed = true;
            }

            _fireHeld = true;
        }

        private void OnFireCanceled(InputAction.CallbackContext context)
        {
            _fireHeld = false;
        }

        private void OnToggleFireModePerformed(InputAction.CallbackContext context)
        {
            _toggleFireModePressed = true;
        }

        private void OnPointerPosition(InputAction.CallbackContext context)
        {
            PointerScreenPosition = context.ReadValue<Vector2>();
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            _reloadPressed = true;
        }

        private void OnWeaponCycle(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<Vector2>().y;
            if (value > 0f)
            {
                _weaponCycleDirection = 1;
            }
            else if (value < 0f)
            {
                _weaponCycleDirection = -1;
            }
        }

        private void OnWeaponSlot(InputAction.CallbackContext context)
        {
            string actionName = context.action.name;
            if (actionName.Length > weaponSlotActionPrefix.Length &&
                int.TryParse(actionName.Substring(weaponSlotActionPrefix.Length), out int slot))
            {
                _weaponSlotPressed = slot;
            }
        }
    }
}
