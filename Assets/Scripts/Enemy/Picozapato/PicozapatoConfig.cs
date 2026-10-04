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

        [Header("Area Attack")]
        [Tooltip("Picozapato only casts the area attack if the target is at least this far. The basic attack always has priority " +
                 "in its range; keep this at or below EnemyStats.attackRange so there is no distance where it can't attack.")]
        [Min(0f)] public float areaMinRange = 1.5f;

        [Tooltip("Maximum distance to the target to cast the area attack.")]
        [Min(0f)] public float areaMaxRange = 9f;

        [Min(0.1f)] public float areaRadius = 2.2f;
        public EffectType areaDamageType = EffectType.PhysicDamage;
        [Min(0f)] public float areaMinDamage = 12f;
        [Min(0f)] public float areaMaxDamage = 18f;

        [Tooltip("Hand plunging into the ground, before the circle appears. The target position is picked at the end of it.")]
        [Min(0f)] public float areaStartupDuration = 0.45f;

        [Tooltip("Circle visible on the ground before it hits: the dodge window. Keep it above radius / player speed + ~0.3s reaction.")]
        [Min(0.1f)] public float areaTelegraphDuration = 1.1f;

        [Tooltip("Pulling the hand out after the hit. Picozapato is vulnerable and doesn't move.")]
        [Min(0f)] public float areaRecoveryDuration = 0.8f;

        [Min(0f)] public float areaCooldown = 5f;

        [Tooltip("Players further than this above/below the circle are not hit (e.g. jumping, ledges).")]
        [Min(0f)] public float areaVerticalTolerance = 1.5f;

        [Header("Area Indicator (visual only)")]
        [Tooltip("Optional prefab. If empty, a simple ring indicator is generated at runtime.")]
        public PicozapatoAreaIndicator areaIndicatorPrefab;

        [Tooltip("Used by the generated indicator. If empty, Sprites/Default is used.")]
        public Material fallbackIndicatorMaterial;

        [Tooltip("Layers used to align the indicator with the ground slope.")]
        public LayerMask groundMask = ~0;

        public Color indicatorColor = new Color(1f, 0.55f, 0.1f, 0.8f);
        public Color indicatorWarningColor = new Color(1f, 0.1f, 0.05f, 1f);

        [Tooltip("How long the indicator flashes after the hit.")]
        [Min(0f)] public float indicatorImpactDuration = 0.35f;

        [Header("Shared Timing")]
        [Tooltip("Random ± fraction applied to every cooldown so Picozapatos never attack in sync. 0.2 = ±20%.")]
        [Range(0f, 0.9f)] public float cooldownVariance = 0.2f;

        [Header("Solo Roaming (when no herd is assigned)")]
        [Tooltip("Radius around the spawn point where random destinations are picked.")]
        [Min(0f)] public float roamRadius = 10f;

        [Tooltip("Random pause (seconds) after reaching a roaming destination. Herds use their own pause.")]
        public Vector2 roamPause = new Vector2(1.5f, 4f);

        [Header("Group Movement")]
        [Tooltip("Minimum distance between a roaming destination and the other members (their destinations and positions).")]
        [Min(0f)] public float herdSpacing = 2.5f;

        [Tooltip("While standing still, Picozapatos closer than this step away from each other.")]
        [Min(0f)] public float separationRadius = 1.8f;

        [Tooltip("How far (meters) a standing Picozapato steps away when someone is too close.")]
        [Min(0f)] public float separationStrength = 1.2f;

        [Tooltip("Random ± fraction applied to each Picozapato's speed. 0.12 = ±12%.")]
        [Range(0f, 0.5f)] public float speedVariance = 0.12f;

        [Header("NavMesh Agent")]
        [Min(0f)] public float acceleration = 10f;
        [Min(0f)] public float angularSpeed = 360f;
        [Min(0f)] public float stoppingDistance = 0.3f;

        [Tooltip("Randomized per Picozapato so local avoidance never deadlocks two equal agents.")]
        public Vector2Int avoidancePriorityRange = new Vector2Int(35, 65);

        public ObstacleAvoidanceType obstacleAvoidance = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        [Header("Audio")]
        public AudioID basicAttackSound = AudioID.PicozapatoBasicAttack;
        public AudioID areaWindupSound = AudioID.PicozapatoAreaWindup;
        public AudioID areaImpactSound = AudioID.PicozapatoAreaImpact;
        public AudioID deathSound = AudioID.PicozapatoDeath;

        public float GetRandomizedCooldown(float baseCooldown) =>
            baseCooldown * (1f + Random.Range(-cooldownVariance, cooldownVariance));

        private void OnValidate()
        {
            areaMaxRange = Mathf.Max(areaMaxRange, areaMinRange);
            areaMaxDamage = Mathf.Max(areaMaxDamage, areaMinDamage);
            roamPause.y = Mathf.Max(roamPause.y, roamPause.x);
            avoidancePriorityRange.x = Mathf.Clamp(avoidancePriorityRange.x, 0, 99);
            avoidancePriorityRange.y = Mathf.Clamp(avoidancePriorityRange.y, avoidancePriorityRange.x, 99);
        }
    }
}
