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
        private UI.HealthAndResourceUI _healthAndResourceUI;

        #endregion

        // TODO: save information:
        // for example current health, level, bufs, skills available, etc
        // Should be saved in a file. --> Make save system

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _playerSkills = GetComponent<PlayerSkills>();
        }
        public override void OnNetworkSpawn()
        {
            // Assign basic skill -> will change, what happen when player choose another button?
            _playerSkills.AssignSkill(SkillKey.LB, _basicSkill);
            _playerSkills.AssignSkill(SkillKey.ONE, _Skill1);
            _resource = _playerStats.maxResource;
            _health = _playerStats.maxHealth;

            _healthAndResourceUI = UI.PlayerUIManager.Instance.GetHealthAndResourceUI();
            _healthAndResourceUI.UpdateHealth(_health, _playerStats.maxHealth);
            _healthAndResourceUI.UpdateResource(_resource, _playerStats.maxResource);
        }

        public void Update()
        {
            if (_resource < _playerStats.maxResource)
            {
                _resource = Mathf.Min(_resource + _playerStats.resourceRegen * Time.deltaTime, _playerStats.maxResource);
                _healthAndResourceUI.UpdateResource(_resource, _playerStats.maxResource);
            }

            if (_health < _playerStats.maxHealth)
            {
                _health = Mathf.Min(_health + _playerStats.healthRegen * Time.deltaTime, _playerStats.maxHealth);
                _healthAndResourceUI.UpdateHealth(_health, _playerStats.maxHealth);
            }
        }

        public bool TryConsumeResource(int cost)
        {
            if (_resource < cost) return false;
            _resource -= cost;
            _healthAndResourceUI.UpdateResource(_resource, _playerStats.maxResource);
            return true;
        }

        public void TakeDamage(float damageAmount, DamageType damageType)
        {
            float reduction = (damageType == DamageType.PhysicDamage) ? _playerStats.physicalDefense : _playerStats.magicDefense;
            reduction = Mathf.Clamp(reduction, 0f, 0.99f);

            float finalDamage = damageAmount * (1 - reduction);
            _health = Mathf.Max(_health - finalDamage, 0f);
            _healthAndResourceUI.UpdateHealth(_health, _playerStats.maxHealth);


            Debug.Log($"Player took {finalDamage} damage. Remaining health: {_health}");
            if (_health <= 0)
            {
                Die();
            }
        }

        public (int,bool) CalculatePhysicalDamage(int minAttackDamage, int maxAttackDamage)
        {
            int attackDamage = Random.Range(minAttackDamage, maxAttackDamage);
            float damage = (attackDamage + _playerStats.bonusPhysicalAttack) * _playerStats.physicalAttackMultiplier;
            bool isCritical = Random.value < _playerStats.criticalChance;
            if (isCritical)
            {
                damage *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(damage), isCritical);
        }

        public (int,bool) CalculateMagicDamage(int minAttackDamage, int maxAttackDamage)
        {
            int attackDamage = Random.Range(minAttackDamage, maxAttackDamage);
            float damage = (attackDamage + _playerStats.bonusMagicAttack) * _playerStats.magicAttackMultiplier;
            bool isCritical = Random.value < _playerStats.criticalChance;
            if (isCritical)
            {
                damage *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(damage), isCritical);
        }

        private void Die()
        {
            Debug.Log("Player has died.");
            // TODO
        }
    }

}