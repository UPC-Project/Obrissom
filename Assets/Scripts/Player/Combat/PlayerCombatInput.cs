using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Obrissom.UI;

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
                if (InputStateManager.Instance != null && InputStateManager.Instance.IsPointerOverUI) return;
                bool activated = _playerSkills.OnSkillPressed(SkillKey.LB);
                if (activated && !isHold) _animator.SetTrigger("basic");
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
                if (InputStateManager.Instance != null && InputStateManager.Instance.IsPointerOverUI) return;
                bool activated = _playerSkills.OnSkillPressed(SkillKey.ONE);
                if (activated && !isHold) _animator.SetTrigger("skill1");
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
            bool isHold = _playerSkills.IsHoldSkill(SkillKey.TWO);

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.TWO))
            {
                if (InputStateManager.Instance != null && InputStateManager.Instance.IsPointerOverUI) return;
                bool activated = _playerSkills.OnSkillPressed(SkillKey.TWO);
                if (activated && !isHold) _animator.SetTrigger("skill2");
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.TWO))
            {
                _playerSkills.OnSkillReleased(SkillKey.TWO);
                if (isHold) _animator.SetTrigger("skill2");
                else _animator.ResetTrigger("skill2");
            }
        }

        public void OnSkill3(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            bool isHold = _playerSkills.IsHoldSkill(SkillKey.THREE);

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.THREE))
            {
                if (InputStateManager.Instance != null && InputStateManager.Instance.IsPointerOverUI) return;
                bool activated = _playerSkills.OnSkillPressed(SkillKey.THREE);
                if (activated && !isHold) _animator.SetTrigger("skill3");
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
            bool isHold = _playerSkills.IsHoldSkill(SkillKey.FOUR);

            if (context.performed && _playerSkills.CanActivateSkill(SkillKey.FOUR))
            {
                if (InputStateManager.Instance != null && InputStateManager.Instance.IsPointerOverUI) return;
                bool activated = _playerSkills.OnSkillPressed(SkillKey.FOUR);
                if (activated && !isHold) _animator.SetTrigger("skill4");
            }

            if (context.canceled && _playerSkills.CanReleaseSkill(SkillKey.FOUR))
            {
                _playerSkills.OnSkillReleased(SkillKey.FOUR);
                if (isHold) _animator.SetTrigger("skill4");
                else _animator.ResetTrigger("skill4");
            }
        }
    }
}

