using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.Player
{
    [DefaultExecutionOrder(-3)]

    public class PlayerLocomotionInput : NetworkBehaviour, PlayerInput.IPlayerLocomotionMapActions
    {
        #region Class variables
        [Header("Player Movement")]
        public bool RunToggledOn { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool CameraPressed { get; private set; }

        private PlayerInput _playerInput;
        public PlayerInput PlayerInput
        {
            get
            {
                if (_playerInput == null)
                {
                    _playerInput = new PlayerInput();
                }
                return _playerInput;
            }
        }
        public Vector2 MovementInput { get; private set; }
        
        [Header("Player Camera")]
        [SerializeField] private float _cameraZoomSpeed;
        [SerializeField, Range(2,4)] private int _cameraZoomMinZoom = 3;
        [SerializeField, Range(4,7)] private int _cameraZoomMaxZoom = 5;
        [SerializeField] private CinemachineThirdPersonFollow _camera;
        public Vector2 LookInput { get; private set; }
        public Vector2 ScrollInput { get; private set; }

        [Header("References")]
        [SerializeField] private Animator _animator;
        

        private void Awake()
        {
            _playerInput = new PlayerInput();
            PlayerInput.Enable();
        }
        #endregion

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            RunToggledOn = false;

            PlayerInput.PlayerLocomotionMap.Enable();
            PlayerInput.PlayerLocomotionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            PlayerInput.PlayerLocomotionMap.Disable();
            PlayerInput.PlayerLocomotionMap.RemoveCallbacks(this);
        }

        private void Update()
        {
            _camera.CameraDistance = Mathf.Clamp(_camera.CameraDistance + ScrollInput.y, _cameraZoomMinZoom, _cameraZoomMaxZoom);
        }

        private void LateUpdate()
        {
            JumpPressed = false;
            ScrollInput = Vector2.zero;
        }

        #region Input callbacks
        public void OnMovement(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            MovementInput = context.ReadValue<Vector2>();
            if (context.performed || context.started) _animator.SetBool("isMoving", true);
            if (context.canceled) _animator.SetBool("isMoving", false);
        }

        public void OnToggleRun(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            RunToggledOn = context.ReadValueAsButton();
        }

        public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            JumpPressed = true;
        }

        public void OnCameraLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }

        public void OnCameraControls(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            CameraPressed = context.ReadValueAsButton();
        }

        public void OnScrollCamera(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            ScrollInput = context.ReadValue<Vector2>().normalized * _cameraZoomSpeed * -1f;
        }
        #endregion
    }
}