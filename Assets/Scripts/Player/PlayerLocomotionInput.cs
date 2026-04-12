using UnityEngine;

namespace Obrissom.Player
{
    [DefaultExecutionOrder(-2)]

    public class PlayerLocomotionInput : MonoBehaviour, PlayerInput.IPlayerLocomotionMapActions
    {
        #region Class variables
        // { get; private set; } for read only
        public bool RunToggledOn { get; private set; }
        public bool JumpPressed { get; private set; } 
        public PlayerInput PlayerInput { get; private set; }
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        #endregion

        private void OnEnable()
        {
            RunToggledOn = false;

            PlayerInput = new PlayerInput();
            PlayerInput.Enable();

            PlayerInput.PlayerLocomotionMap.Enable();
            PlayerInput.PlayerLocomotionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            //PlayerInput.PlayerLocomotionMap.Disable();
            PlayerInput.PlayerLocomotionMap.RemoveCallbacks(this);
        }

        private void LateUpdate()
        {
            JumpPressed = false;
        }

        #region Input callbacks
        public void OnMovement(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            MovementInput = context.ReadValue<Vector2>();
        }

        public void OnLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }


        public void OnToggleRun(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                RunToggledOn = true;
            }
            else if (context.canceled)
            {
                RunToggledOn = false;
            }
        }

        public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
           if(!context.performed) return;

            JumpPressed = true;
        }
        #endregion
    }
}