using UnityEngine;
using Obrissom.Combat;


namespace Obrissom.Enemy
{
    /// <summary>
    /// Configurable data for an enemy type. Create one asset per enemy type.
    /// </summary>
    [CreateAssetMenu(fileName = "New EnemyStats", menuName = "Obrissom/Enemy/EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        [Min(1f)] public float maxHealth = 100f;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 3.5f;
        [Min(0f)] public float chaseRange = 10f;

        [Header("Combat")]
        public DamageTypeEnemy damageType = DamageTypeEnemy.Physical;
        [Min(0f)] public float minAttackDamage = 8f;
        [Min(0f)] public float maxAttackDamage = 15f;
        [Min(0f)] public float attackRange = 1.5f;
        [Min(0f)] public float attackCooldown = 1.5f;

        [Header("Defense")]
        [Range(0f, 0.99f)] public float physicalDefense = 0f;
        [Range(0f, 0.99f)] public float magicDefense = 0f;

        [Header("Rewards")]
        [Min(0)] public int experienceReward = 10;
        public LootEntry[] lootTable;
    }
}