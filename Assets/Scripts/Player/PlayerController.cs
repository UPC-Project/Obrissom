using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : NetworkBehaviour
    {
        #region Class variables

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private GameObject _playerCamera;
        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerStats _playerStats;


        [Header("Movement")]
        public float walkAcceleration = 35f;
        public float walkSpeed = 4f;
        public float runAcceleration = 50f;
        public float runSpeed = 7f;
        public float airAcceleration = 4f;
        public float drag = 15f;
        public float jumpSpeed = 1f;
        public float playerRotationSpeed = 1f;
        [SerializeField] private float _gravity = 9.81f;
        [SerializeField] private bool _isGrounded;
        [SerializeField] private float _groundCheckDistance = 0.4f;
        [SerializeField] private float _radius = 0.23f;
        [SerializeField] private Vector3 _originGroundCheck;
        [SerializeField] private float _verticalVelocity;
        [SerializeField] private Vector3 _newVelocity;
        private Vector3 _hitNormal;
        private float _slopeTolerance = 0.45f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 50f;

        private Vector2 _cameraRotation = Vector2.zero;

        #endregion

        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerStats = GetComponent<PlayerStats>();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            _originGroundCheck = transform.position + Vector3.up * 0.5f;
            _isGrounded = Physics.SphereCast(
                            _originGroundCheck,
                            _radius,
                            Vector3.down,
                            out RaycastHit hit,
                            _groundCheckDistance);
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleHorizontalMovement();

            HandleVerticalMovement();

            // Slide on slopes
            if (_hitNormal != Vector3.zero && _hitNormal.y < _slopeTolerance)
            {
                _newVelocity = Vector3.ProjectOnPlane(_newVelocity, _hitNormal);
            }

            // Unity suggests only calling this once per tick
            _characterController.Move(_newVelocity * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            // Movement it's updated, now define camera rotation based on input
            if (_playerLocomotionInput.CameraPressed)
            {
                _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;
                _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);
            }

            _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);
        }

        private void HandleHorizontalMovement()
        {
            // Check if is running or walking
            float speed = !_isGrounded ? airAcceleration : (_playerLocomotionInput.RunToggledOn ? runSpeed : walkSpeed);
            speed += _playerStats.bonusSpeed;
            float acceleration = _playerLocomotionInput.RunToggledOn ? runAcceleration : walkAcceleration;

            // Update movement
            Vector3 cameraForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(_playerCamera.transform.right.x, 0f, _playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = cameraForwardXZ * _playerLocomotionInput.MovementInput.y + cameraRightXZ * _playerLocomotionInput.MovementInput.x;

            if (movementDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    playerRotationSpeed * 360f * Time.deltaTime
                );
            }

            Vector3 movementDelta = movementDirection * acceleration * Time.deltaTime;
            _newVelocity = _characterController.velocity + movementDelta;

            // Drag
            Vector3 currentDrag = _newVelocity.normalized * drag * Time.deltaTime;
            _newVelocity = (_newVelocity.magnitude > drag * Time.deltaTime) ? _newVelocity - currentDrag : Vector3.zero;
            // Clamp
            Vector3 horizontal = new Vector3(_newVelocity.x, 0f, _newVelocity.z);
            horizontal = Vector3.ClampMagnitude(horizontal, speed);
            _newVelocity.x = horizontal.x;
            _newVelocity.z = horizontal.z;
        }


        private void HandleVerticalMovement()
        {
            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -5f;
            }

            _verticalVelocity -= _gravity * Time.deltaTime;

            if (_playerLocomotionInput.JumpPressed && _isGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(jumpSpeed * 3 * _gravity);
            }

            _newVelocity.y = _verticalVelocity;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.tag == "Stairs")
            {
                _hitNormal = Vector3.zero;
            }
            else
            {
                _hitNormal = hit.normal;
            }
        }

        // NETWORKING
        public override void OnNetworkSpawn()
        {
            _playerCamera.SetActive(IsOwner);
        }

    }
}
