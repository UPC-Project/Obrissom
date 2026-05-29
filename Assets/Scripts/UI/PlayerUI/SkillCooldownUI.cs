using Obrissom.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    public class SkillCooldownUI : MonoBehaviour
    {
        [SerializeField] private Image _lbCooldown;
        [SerializeField] private Image _skill1Cooldown;
        [SerializeField] private Image _skill2Cooldown;
        [SerializeField] private Image _skill3Cooldown;
        [SerializeField] private Image _skill4Cooldown;

        private PlayerSkills _playerSkills;

        public void SetPlayerSkills(PlayerSkills playerSkills)
        {
            _playerSkills = playerSkills;
        }

        private void Update()
        {
            if (_playerSkills == null) return;

            _lbCooldown.fillAmount = _playerSkills.GetCooldownPercent(SkillKey.LB);
            _skill1Cooldown.fillAmount = _playerSkills.GetCooldownPercent(SkillKey.ONE);
            _skill2Cooldown.fillAmount = _playerSkills.GetCooldownPercent(SkillKey.TWO);
            _skill3Cooldown.fillAmount = _playerSkills.GetCooldownPercent(SkillKey.THREE);
            _skill4Cooldown.fillAmount = _playerSkills.GetCooldownPercent(SkillKey.FOUR);
        }
    }
}