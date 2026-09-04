using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.Player
{
    [DefaultExecutionOrder(-2)]
    public class PlayerCombatInput : MonoBehaviour, PlayerInput.IPlayerSkillMapActions
    {
        private PlayerSkills _playerSkills;
        private PlayerInput _playerInput;
        private NetworkObject _networkObject;
        public Animator _animator;

        private bool IsOwner => _networkObject != null && _networkObject.IsOwner;

        private void Awake()
        {
            _playerSkills = GetComponent<PlayerSkills>();
            _playerInput = GetComponent<PlayerLocomotionInput>().PlayerInput;
            _networkObject = GetComponent<NetworkObject>();
        }
        private void OnEnable()
        {
            _playerInput.PlayerSkillMap.Enable();
            _playerInput.PlayerSkillMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            _playerInput.PlayerSkillMap.Disable();
            _playerInput.PlayerSkillMap.RemoveCallbacks(this);
        }

        public void OnBasic(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            if (context.performed) 
            {
                _playerSkills.OnSkillPressed(SkillKey.LB);
                _animator.SetTrigger("basic");
            }

            if (context.canceled) 
            {
                _playerSkills.OnSkillReleased(SkillKey.LB);
                _animator.ResetTrigger("basic");
            }
            
        }

        public void OnSkill1(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            if (context.performed)
            {
                _playerSkills.OnSkillPressed(SkillKey.ONE);
                _animator.SetTrigger("skill1");
            }

            if (context.canceled)
            {
                _playerSkills.OnSkillReleased(SkillKey.ONE);
                _animator.ResetTrigger("skill1");
            }
        }

        public void OnSkill2(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.TWO);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.TWO);
        }

        public void OnSkill3(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.THREE);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.THREE);
        }

        public void OnSkill4(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.FOUR);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.FOUR);
        }
    }
}
