using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        #region Class variables

        [SerializeField, Min(0)] private float _health;
        [SerializeField, Min(0)] private float _baseDamage;
        [SerializeField, Min(0)] private float _resource; // Mana / Stamina / Fury, etc
        [SerializeField] private bool _isUsingSkill;

        private PlayerStats _playerStats;

        #endregion

        // TODO: save information:
        // for example current health, level, bufs, skills available, etc
        // Should be saved in a file. --> Make save system

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
        }


        public void Attack(Skill skill)
        {
            if (_isUsingSkill) return;

            _isUsingSkill = true;
            Debug.Log($"Player is attacking with {skill.skillName}");
        }

        public void TakeDamage(float damageAmount, DamageType damageType)
        {
            float reduction = (damageType == DamageType.PhysicalDamage) ? _playerStats.physicalDefense : _playerStats.magicDefense;
            reduction = Mathf.Clamp(reduction, 0f, 0.99f);

            float finalDamage = damageAmount * (1 - reduction);
            _health = Mathf.Max(_health - finalDamage, 0f);
            Debug.Log($"Player took {finalDamage} damage. Remaining health: {_health}");
            if (_health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Player has died.");
            // TODO
        }
    }

}