using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerSkills : NetworkBehaviour
    {
        [SerializeField] private Dictionary<SkillKey, Skill> _activeSkills = new Dictionary<SkillKey, Skill>();
        [SerializeField] private static Skill _basicSkill;

        private Dictionary<SkillKey, float> _cooldowns = new Dictionary<SkillKey, float>();

        private Skill _activeSkill = null;

        [SerializeField] private float _aimRotationSpeed = 4f;

        private PlayerCombat _playerCombat;
        

        private void Start()
        {
            _playerCombat = GetComponent<PlayerCombat>();
        }

        public override void OnNetworkSpawn()
        { 
            if (!IsOwner) return;
            UI.PlayerUIManager.Instance.RegisterPlayer(this);
        }

        private void Update()
        {
            foreach (SkillKey key in _cooldowns.Keys.ToList())
            {
                if (_cooldowns[key] > 0f) _cooldowns[key] -= Time.deltaTime;
            }

            // Hold skill is being hold
            if (_activeSkill != null && _activeSkill.behaviour.castType == CastType.Hold)
            {
                Vector3 aimPosition = _playerCombat.GetAimPosition();

                // update Hold skill
                _activeSkill.behaviour.OnHoldUpdate(gameObject, _activeSkill, aimPosition);

                // Rotate player to crosshair
                Vector3 direction = new Vector3(aimPosition.x - transform.position.x, 0f, aimPosition.z - transform.position.z).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _aimRotationSpeed * 360f * Time.deltaTime);
                }
            }
        }

        public void UnlockSkill(Skill skill)
        {
            SkillKey? key = GetNextAvailableKey();
            if (key == null) return;
          
            _activeSkills[key.Value] = skill;
        }

        private SkillKey? GetNextAvailableKey()
        {
            foreach (SkillKey key in System.Enum.GetValues(typeof(SkillKey)))
                if (!_activeSkills.ContainsKey(key)) return key;
            return null;
        }

        public void OnSkillPressed(SkillKey key)
        {
            if (!CanActivateSkill(key)) return;

            _activeSkills.TryGetValue(key, out Skill skill);

            _activeSkill = skill;

            if (skill.behaviour.castType == CastType.Hold)
            {
                skill.behaviour.OnHold(gameObject, skill, Vector3.zero); // pass target vector
            }
            else
            {
                _cooldowns[key] = skill.cooldownTime;
                skill.behaviour.Execute(gameObject, skill, Vector3.zero); // pass target vector
            }
        }

        public void OnSkillReleased(SkillKey key)
        {
            if (!CanReleaseSkill(key)) return;
            if (_activeSkill.behaviour.castType == CastType.Hold)
            {
                Vector3 aimPosition = _playerCombat.GetAimPosition();
                _cooldowns[key] = _activeSkill.cooldownTime;
                _activeSkill.behaviour.OnRelease(gameObject, _activeSkill, aimPosition);
            }
            else
            {
                _activeSkill.behaviour.OnRelease(gameObject, _activeSkill, Vector3.zero);
            }
            _activeSkill = null;
        }

        private bool CanActivateSkill(SkillKey key)
        {
            if (_activeSkill != null) return false;
            if (!_activeSkills.TryGetValue(key, out Skill skill)) return false;

            if (_cooldowns.TryGetValue(key, out float remaining) && remaining > 0f) return false;

            if (!_playerCombat.TryConsumeResource(_activeSkills[key].cost)) return false;

            return true;
        }

        private bool CanReleaseSkill(SkillKey key)
        {
            if (_activeSkill == null) return false;
            if (!_activeSkills.TryGetValue(key, out Skill skill)) return false;
            return skill == _activeSkill;
        }

        public float GetCooldownPercent(SkillKey key)
        {
            if (!_activeSkills.TryGetValue(key, out Skill skill)) return 0f;
            if (!_cooldowns.TryGetValue(key, out float remaining)) return 0f;
            return remaining / skill.cooldownTime;
        }

    }
}
