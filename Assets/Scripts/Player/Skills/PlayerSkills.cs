using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerSkills : NetworkBehaviour
    {
        [SerializeField] private Dictionary<SkillKey, Skill> _activeSkills = new Dictionary<SkillKey, Skill>();
        private Dictionary<SkillKey, float> _cooldowns = new Dictionary<SkillKey, float>();
        private Skill _activeSkill = null;
        public bool IsUsingSkill => _activeSkill != null;

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
        }

        public void UnlockSkill(Skill skill)
        {
            // TODO
        }

        public void AssignSkill(SkillKey key, Skill skill)
        {
            _activeSkills[key] = skill;
        }

        public void OnSkillPressed(SkillKey key)
        {
            if (!_activeSkills.TryGetValue(key, out Skill skill)) return;

            if (_cooldowns.TryGetValue(key, out float remaining) && remaining > 0f) return;

            if (!_playerCombat.TryConsumeResource(skill.cost)) return;

            _activeSkill = skill;
            _cooldowns[key] = skill.cooldownTime;

            if (skill.behaviour.castType == CastType.Hold)
            {
                skill.behaviour.OnHold(gameObject, skill, Vector3.zero); // pass target vector
            }
            else
            {
                skill.behaviour.Execute(gameObject, skill, Vector3.zero); // pass target vector
            }
        }

        public void OnSkillReleased(SkillKey key)
        {
            if (_activeSkill != null)
            {
                _activeSkill.behaviour.OnRelease(gameObject, _activeSkill, Vector3.zero); // pass target vector
                _activeSkill = null;
            }
        }

        public float GetCooldownPercent(SkillKey key)
        {
            if (!_activeSkills.TryGetValue(key, out Skill skill)) return 0f;
            if (!_cooldowns.TryGetValue(key, out float remaining)) return 0f;
            return remaining / skill.cooldownTime;
        }

    }
}
