using Obrissom.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Obrissom.UI
{
    public class InputStateManager : MonoBehaviour
    {
        public static InputStateManager Instance { get; private set; }

        private PlayerInput _playerInput;
        private bool _isOverUI = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterPlayerInput(PlayerInput input)
        {
            _playerInput = input;
            
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _isOverUI = true;
                BlockUIConflictingInputs();
            }
        }

        private void Update()
        {
            if (_playerInput == null) return;
            bool isCurrentlyOverUI = EventSystem.current.IsPointerOverGameObject();

            if (isCurrentlyOverUI != _isOverUI)
            {
                _isOverUI = isCurrentlyOverUI;
                if (_isOverUI)
                {
                    BlockUIConflictingInputs();
                }
                else
                {
                    UnblockUIConflictingInputs();
                }
            }
        }

        private void BlockUIConflictingInputs()
        {
            _playerInput.PlayerSkillMap.Disable();
            
            // Player can still move, jump and sprint, only camera input deactivated
            _playerInput.PlayerLocomotionMap.CameraControls.Disable();
            _playerInput.PlayerLocomotionMap.CameraLook.Disable();
            _playerInput.PlayerLocomotionMap.ScrollCamera.Disable();
        }

        private void UnblockUIConflictingInputs()
        {
            _playerInput.PlayerSkillMap.Enable();
            
            _playerInput.PlayerLocomotionMap.CameraControls.Enable();
            _playerInput.PlayerLocomotionMap.CameraLook.Enable();
            _playerInput.PlayerLocomotionMap.ScrollCamera.Enable();
        }
    }
}

