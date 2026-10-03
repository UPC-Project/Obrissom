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
            bool isHold = _playerSkills.IsHoldSkill(SkillKey.LB);

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.LB))
            {
                _playerSkills.OnSkillPressed(SkillKey.LB);
                if (!isHold) _animator.SetTrigger("basic");
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.LB))
            {
                _playerSkills.OnSkillReleased(SkillKey.LB);
                if (isHold) _animator.SetTrigger("basic");
                else _animator.ResetTrigger("basic");
            }
        }

        public void OnSkill1(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            bool isHold = _playerSkills.IsHoldSkill(SkillKey.ONE);

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.ONE))
            {
                _playerSkills.OnSkillPressed(SkillKey.ONE);
                if (!isHold) _animator.SetTrigger("skill1");
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.ONE))
            {
                _playerSkills.OnSkillReleased(SkillKey.ONE);
                if (isHold) _animator.SetTrigger("skill1");
                else _animator.ResetTrigger("skill1");
            }
        }

        public void OnSkill2(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.TWO))
            {
                _playerSkills.OnSkillPressed(SkillKey.TWO);
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.TWO))
            {
                _playerSkills.OnSkillReleased(SkillKey.TWO);
            }
        }

        public void OnSkill3(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            bool isHold = _playerSkills.IsHoldSkill(SkillKey.THREE);

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.THREE))
            {
                _playerSkills.OnSkillPressed(SkillKey.THREE);
                if (!isHold) _animator.SetTrigger("skill3");
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.THREE))
            {
                _playerSkills.OnSkillReleased(SkillKey.THREE);
                if (isHold) _animator.SetTrigger("skill3");
                else _animator.ResetTrigger("skill3");
            }
        }

        public void OnSkill4(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.FOUR))
            {
                _playerSkills.OnSkillPressed(SkillKey.FOUR);
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.FOUR))
            {
                _playerSkills.OnSkillReleased(SkillKey.FOUR);
            }
        }
    }
}
