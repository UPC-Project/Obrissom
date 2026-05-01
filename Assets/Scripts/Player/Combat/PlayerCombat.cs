using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
        #region Class variables

        [SerializeField, Min(0)] private float _health;
        [SerializeField, Min(0)] private float _resource; // Mana / Stamina / Fury, etc

        [SerializeField] private Skill _basicSkill;
        [SerializeField] private Skill _Skill1; // Will change -> gained by leveling up

        private PlayerStats _playerStats;
        private PlayerSkills _playerSkills;

        #endregion

        // TODO: save information:
        // for example current health, level, bufs, skills available, etc
        // Should be saved in a file. --> Make save system

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _playerSkills = GetComponent<PlayerSkills>();
        }

        public void Update()
        {
        }


        public override void OnNetworkSpawn()
        {
            // Assign basic skill -> will change, what happen when player choose another button?
            _playerSkills.AssignSkill(SkillKey.LB, _basicSkill);
            _playerSkills.AssignSkill(SkillKey.ONE, _Skill1);
        }

        public void TakeDamage(float damageAmount, DamageType damageType)
        {
            float reduction = (damageType == DamageType.PhysicDamage) ? _playerStats.physicalDefense : _playerStats.magicDefense;
            reduction = Mathf.Clamp(reduction, 0f, 0.99f);

            float finalDamage = damageAmount * (1 - reduction);
            _health = Mathf.Max(_health - finalDamage, 0f);
            Debug.Log($"Player took {finalDamage} damage. Remaining health: {_health}");
            if (_health <= 0)
            {
                Die();
            }
        }

        public int CalculatePhysicalDamage(int attackDamage)
        {
            float damage = (attackDamage + _playerStats.bonusPhysicalAttack) * _playerStats.physicalAttackMultiplier;
            bool isCritical = Random.value < _playerStats.criticalChance;
            if (isCritical)
            {
                damage *= _playerStats.criticalDamage;
            }
            return Mathf.RoundToInt(damage);
        }

        public int CalculateMagicDamage(int attackDamage)
        {
            float damage = (attackDamage + _playerStats.bonusMagicAttack) * _playerStats.magicAttackMultiplier;
            bool isCritical = Random.value < _playerStats.criticalChance;
            if (isCritical)
            {
                damage *= _playerStats.criticalDamage;
            }
            return Mathf.RoundToInt(damage);
        }

        private void Die()
        {
            Debug.Log("Player has died.");
            // TODO
        }
    }

}