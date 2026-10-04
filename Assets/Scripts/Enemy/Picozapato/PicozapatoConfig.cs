using Obrissom.Audio;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Picozapato-specific configuration. Assign one asset per variant.
    /// General stats live in EnemyStats: health, moveSpeed, chaseRange (detection),
    /// and the basic attack (attackRange, attackCooldown, min/maxAttackDamage, damageType).
    /// </summary>
    [CreateAssetMenu(fileName = "New PicozapatoConfig", menuName = "Obrissom/Enemy/PicozapatoConfig")]
    public class PicozapatoConfig : ScriptableObject
    {
        [Header("Basic Attack (range, cooldown and damage come from EnemyStats)")]
        [Tooltip("Full angle of the frontal hit in degrees.")]
        [Range(0f, 360f)] public float basicAttackAngle = 110f;

        [Tooltip("Time between starting the attack animation and the hit. The player's reaction window.")]
        [Min(0f)] public float basicWindupDuration = 0.35f;

        [Tooltip("Time after the hit before Picozapato can act again.")]
        [Min(0f)] public float basicRecoveryDuration = 0.45f;

        [Header("Shared Timing")]
        [Tooltip("Random ± fraction applied to every cooldown so Picozapatos never attack in sync. 0.2 = ±20%.")]
        [Range(0f, 0.9f)] public float cooldownVariance = 0.2f;

        [Header("Roaming")]
        [Tooltip("Radius around the spawn point where random destinations are picked.")]
        [Min(0f)] public float roamRadius = 10f;

        [Tooltip("Random pause (seconds) after reaching a roaming destination.")]
        public Vector2 roamPause = new Vector2(1.5f, 4f);

        [Header("NavMesh Agent")]
        [Min(0f)] public float acceleration = 10f;
        [Min(0f)] public float angularSpeed = 360f;
        [Min(0f)] public float stoppingDistance = 0.3f;

        [Tooltip("Randomized per Picozapato so local avoidance never deadlocks two equal agents.")]
        public Vector2Int avoidancePriorityRange = new Vector2Int(35, 65);

        public ObstacleAvoidanceType obstacleAvoidance = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        [Header("Audio")]
        public AudioID basicAttackSound = AudioID.PicozapatoBasicAttack;
        public AudioID deathSound = AudioID.PicozapatoDeath;

        public float GetRandomizedCooldown(float baseCooldown) =>
            baseCooldown * (1f + Random.Range(-cooldownVariance, cooldownVariance));

        private void OnValidate()
        {
            roamPause.y = Mathf.Max(roamPause.y, roamPause.x);
            avoidancePriorityRange.x = Mathf.Clamp(avoidancePriorityRange.x, 0, 99);
            avoidancePriorityRange.y = Mathf.Clamp(avoidancePriorityRange.y, avoidancePriorityRange.x, 99);
        }
    }
}
