using Obrissom.Audio;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Picozapato settings. Health, speed, detection and the basic attack come from EnemyStats.
    /// </summary>
    [CreateAssetMenu(fileName = "New PicozapatoConfig", menuName = "Obrissom/Enemy/PicozapatoConfig")]
    public class PicozapatoConfig : ScriptableObject
    {
        [Header("Basic Attack (range, cooldown and damage in EnemyStats)")]
        [Tooltip("Full angle of the hit, in degrees.")]
        [Range(0f, 360f)] public float basicAttackAngle = 110f;

        [Tooltip("Time before the hit.")]
        [Min(0f)] public float basicWindupDuration = 0.35f;

        [Tooltip("Time after the hit.")]
        [Min(0f)] public float basicRecoveryDuration = 0.45f;

        [Header("Area Attack")]
        [Tooltip("Keep it at or below EnemyStats.attackRange.")]
        [Min(0f)] public float areaMinRange = 1.5f;

        [Min(0f)] public float areaMaxRange = 9f;

        [Min(0.1f)] public float areaRadius = 2.2f;
        public EffectType areaDamageType = EffectType.PhysicDamage;
        [Min(0f)] public float areaMinDamage = 12f;
        [Min(0f)] public float areaMaxDamage = 18f;

        [Tooltip("Time before the circle appears.")]
        [Min(0f)] public float areaStartupDuration = 0.45f;

        [Tooltip("Time the circle stays before it hits. This is the dodge window.")]
        [Min(0.1f)] public float areaTelegraphDuration = 1.1f;

        [Tooltip("Time after the hit.")]
        [Min(0f)] public float areaRecoveryDuration = 0.8f;

        [Min(0f)] public float areaCooldown = 5f;

        [Tooltip("Max height difference to be hit.")]
        [Min(0f)] public float areaVerticalTolerance = 1.5f;

        [Header("Area Indicator")]
        [Tooltip("Optional. If empty, a simple ring is created.")]
        public PicozapatoAreaIndicator areaIndicatorPrefab;

        [Tooltip("Material for the simple ring. If empty, Sprites/Default is used.")]
        public Material fallbackIndicatorMaterial;

        [Tooltip("Ground layers for the indicator.")]
        public LayerMask groundMask = ~0;

        public Color indicatorColor = new Color(1f, 0.55f, 0.1f, 0.8f);
        public Color indicatorWarningColor = new Color(1f, 0.1f, 0.05f, 1f);

        [Min(0f)] public float indicatorImpactDuration = 0.35f;

        [Header("Interruption")]
        [Tooltip("A hit during the windup cancels the basic attack.")]
        public bool basicInterruptible = false;

        [Tooltip("A hit before the roots land cancels the area attack.")]
        public bool areaInterruptible = true;

        [Tooltip("Smaller hits don't interrupt. 0 = any hit.")]
        [Min(0f)] public float minDamageToInterrupt = 0f;

        [Tooltip("Part of the cooldown used after an interrupt.")]
        [Range(0f, 1f)] public float interruptedCooldownFactor = 0.5f;

        [Header("Timing")]
        [Tooltip("Random ± change on every cooldown. 0.2 = ±20%.")]
        [Range(0f, 0.9f)] public float cooldownVariance = 0.2f;

        [Header("Solo Roaming (no herd)")]
        [Min(0f)] public float roamRadius = 10f;

        [Tooltip("Random pause at each destination, in seconds.")]
        public Vector2 roamPause = new Vector2(1.5f, 4f);

        [Header("Group Movement")]
        [Tooltip("Min distance between roaming destinations.")]
        [Min(0f)] public float herdSpacing = 2.5f;

        [Tooltip("Enemies closer than this step away.")]
        [Min(0f)] public float separationRadius = 1.8f;

        [Tooltip("How far to step away.")]
        [Min(0f)] public float separationStrength = 1.2f;

        [Tooltip("Random ± change on speed. 0.12 = ±12%.")]
        [Range(0f, 0.5f)] public float speedVariance = 0.12f;

        [Header("Targeting")]
        [Tooltip("Once in combat, chase up to chaseRange × this.")]
        [Min(1f)] public float engagedRangeMultiplier = 1.6f;

        [Tooltip("Seconds between target checks.")]
        [Min(0.2f)] public float retargetInterval = 1f;

        [Tooltip("Extra meters per enemy already on a player. Spreads the group.")]
        [Min(0f)] public float targetCrowdPenalty = 3f;

        [Tooltip("How much better a new target must be to switch.")]
        [Min(0f)] public float targetSwitchHysteresis = 2f;

        [Header("Combat Position")]
        [Tooltip("Each one picks its distance in this range. Keep it inside the area range.")]
        public Vector2 preferredRange = new Vector2(4.5f, 7.5f);

        [Tooltip("Min angle between enemies around a player.")]
        [Range(0f, 180f)] public float minAngleBetweenEngagers = 35f;

        [Tooltip("Move in to peck while the area attack is on cooldown.")]
        public bool closeInWhileAreaOnCooldown = true;

        [Tooltip("Only move in if the area attack needs more than this, in seconds.")]
        [Min(0f)] public float closeInMinAreaCooldownLeft = 2f;

        [Tooltip("Extra enemies wait further away.")]
        [Min(1)] public int maxEngagedPerTarget = 4;

        [Min(0f)] public float outerRingExtraDistance = 3.5f;

        [Tooltip("Min destination change before a new path.")]
        [Min(0f)] public float repathThreshold = 0.4f;

        [Header("Attack Limits (per player)")]
        [Tooltip("Max attacks at the same time on one player.")]
        [Min(1)] public int maxSimultaneousAttacksPerTarget = 2;

        [Tooltip("Min seconds between attacks on one player.")]
        [Min(0f)] public float minTimeBetweenAttacksOnTarget = 0.6f;

        [Tooltip("Random wait before the first attack, in seconds.")]
        public Vector2 engageReactionDelay = new Vector2(0.4f, 1.2f);

        [Header("NavMesh Agent")]
        [Min(0f)] public float acceleration = 10f;
        [Min(0f)] public float angularSpeed = 360f;
        [Min(0f)] public float stoppingDistance = 0.3f;

        [Tooltip("Random per enemy, avoids agents blocking each other.")]
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
            preferredRange.y = Mathf.Max(preferredRange.y, preferredRange.x);
            engageReactionDelay.y = Mathf.Max(engageReactionDelay.y, engageReactionDelay.x);

            if (preferredRange.x < areaMinRange || preferredRange.y > areaMaxRange)
                Debug.LogWarning($"[PicozapatoConfig] {name}: preferredRange should be inside the area range.", this);
        }
    }
}
