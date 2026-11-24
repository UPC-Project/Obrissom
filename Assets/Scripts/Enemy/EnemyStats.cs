using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Configurable data for an enemy type. Create one asset per enemy type.
    /// </summary>
    [CreateAssetMenu(fileName = "New EnemyStats", menuName = "Obrissom/Enemy/EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Tooltip("Used to indicate player enemy name (in quests for example)")] public string enemyName;

        [Header("Health")]
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Seconds out of combat before health starts coming back.")]
        [Min(0f)] public float regenDelay = 5f;
        [Tooltip("Part of max health recovered per second out of combat (0.1 = 10%). 0 = no regen.")]
        [Min(0f)] public float regenPerSecond = 0.1f;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 3.5f;
        [Min(0f)] public float chaseRange = 10f;
        [Tooltip("moveSpeed multiplier while chasing.")]
        [Min(0f)] public float chaseSpeedMultiplier = 1.5f;

        [Header("Combat")]
        public EffectType damageType = EffectType.PhysicDamage;
        [Min(0f)] public float minAttackDamage = 8f;
        [Min(0f)] public float maxAttackDamage = 15f;
        [Min(0f)] public float attackRange = 1.5f;
        [Min(0f)] public float attackCooldown = 1.5f;

        [Header("Defense")]
        [Range(0f, 0.99f)] public float physicalDefense = 0f;
        [Range(0f, 0.99f)] public float magicDefense = 0f;

        [Header("Rewards")]
        [Min(0)] public int experienceReward = 10;
        [Tooltip("One random roll: drops a single entry by weight.")]
        public LootEntry[] lootTable;
        [Tooltip("Always dropped on death, on top of the loot table roll. Weight is ignored.")]
        public LootEntry[] guaranteedLoot;
    }
}