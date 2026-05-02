using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.Player
{
    [DefaultExecutionOrder(-2)]
    public class PlayerCombatInput : MonoBehaviour, PlayerInput.IPlayerSkillMapActions
    {
        private PlayerSkills _playerSkills;
        private PlayerInput _playerInput;
        private void Awake()
        {
            _playerSkills = GetComponent<PlayerSkills>();
            _playerInput = GetComponent<PlayerLocomotionInput>().PlayerInput;
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

        public void OnBasic(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.LB);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.LB);
        }

        public void OnSkill1(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.ONE);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.ONE);
        }

        public void OnSkill2(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.TWO);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.TWO);
        }

        public void OnSkill3(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.THREE);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.THREE);
        }

        public void OnSkill4(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (context.performed) _playerSkills.OnSkillPressed(SkillKey.FOUR);
            if (context.canceled) _playerSkills.OnSkillReleased(SkillKey.FOUR);
        }
    }
}
