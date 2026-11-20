using System;
using Obrissom.Audio;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Spectre tank settings. Health, speed, detection and the dash damage come from EnemyStats.
    /// </summary>
    [CreateAssetMenu(fileName = "New SpectreTankConfig", menuName = "Obrissom/Enemy/SpectreTankConfig")]
    public class SpectreTankConfig : ScriptableObject
    {
        [Header("Dash (start range, cooldown and damage in EnemyStats)")]
        [Tooltip("Max dash length. Walls cut it shorter.")]
        [Min(0f)] public float dashDistance = 8f;

        [Tooltip("Width of the strip.")]
        [Min(0.1f)] public float dashWidth = 2.5f;

        [Tooltip("Extra hit length past the end of the dash (the front of the body).")]
        [Min(0f)] public float dashReach = 1f;

        [Tooltip("Time to turn to the target before the strip appears.")]
        [Min(0f)] public float dashTurnDuration = 0.3f;

        [Tooltip("Time the strip shows before the dash. This is the dodge window.")]
        [Min(0.1f)] public float dashTelegraphDuration = 1f;

        [Tooltip("Time the dash takes to cross the strip.")]
        [Min(0.05f)] public float dashDuration = 0.35f;

        [Tooltip("Time standing still after the dash.")]
        [Min(0f)] public float dashRecoveryDuration = 1f;

        [Header("Ground Slam")]
        [Tooltip("Slams when the target is this close.")]
        [Min(0f)] public float slamTriggerRange = 4f;

        [Tooltip("Time before the first circle appears.")]
        [Min(0f)] public float slamStartupDuration = 0.4f;

        [Tooltip("Time standing still after the last circle.")]
        [Min(0f)] public float slamRecoveryDuration = 1.2f;

        [Min(0f)] public float slamCooldown = 8f;

        public EffectType slamDamageType = EffectType.PhysicDamage;

        [Tooltip("Max height difference to be hit.")]
        [Min(0f)] public float slamVerticalTolerance = 1.5f;

        [Tooltip("Circles in order, from the small one to the big one. Each one hits everyone inside it.")]
        public SlamStage[] slamStages =
        {
            new SlamStage(2.5f, 0.8f, 6f, 8f),
            new SlamStage(4.5f, 0.7f, 9f, 12f),
            new SlamStage(6.5f, 0.7f, 13f, 17f, 1.5f),
        };

        [Header("Ground Warning")]
        [Tooltip("If empty, Sprites/Default is used.")]
        public Material telegraphMaterial;

        [Tooltip("Ground layers for the warning.")]
        public LayerMask groundMask = ~0;

        public Color telegraphColor = new Color(1f, 0.55f, 0.1f, 0.8f);
        public Color telegraphWarningColor = new Color(1f, 0.1f, 0.05f, 1f);

        [Tooltip("Progress (0-1) where it starts flashing.")]
        [Range(0f, 1f)] public float warningThreshold = 0.75f;

        [Tooltip("Time the warning fades after the hit.")]
        [Min(0f)] public float impactFadeDuration = 0.4f;

        [Header("Solo Roaming (no herd)")]
        [Min(0f)] public float roamRadius = 10f;

        [Tooltip("Random pause at each destination, in seconds.")]
        public Vector2 roamPause = new Vector2(2f, 5f);

        [Header("Group Movement")]
        [Tooltip("Min distance between roaming destinations.")]
        [Min(0f)] public float herdSpacing = 3f;

        [Tooltip("Enemies closer than this step away.")]
        [Min(0f)] public float separationRadius = 2.2f;

        [Tooltip("How far to step away.")]
        [Min(0f)] public float separationStrength = 1.2f;

        [Tooltip("Random ± change on speed. 0.1 = ±10%.")]
        [Range(0f, 0.5f)] public float speedVariance = 0.1f;

        [Header("Targeting")]
        [Tooltip("Once in combat, chase up to chaseRange × this.")]
        [Min(1f)] public float engagedRangeMultiplier = 1.4f;

        [Header("NavMesh Agent")]
        [Min(0f)] public float acceleration = 8f;
        [Min(0f)] public float angularSpeed = 240f;
        [Min(0f)] public float stoppingDistance = 1.5f;

        [Tooltip("Random per enemy, avoids agents blocking each other.")]
        public Vector2Int avoidancePriorityRange = new Vector2Int(35, 65);

        public ObstacleAvoidanceType obstacleAvoidance = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        [Header("Audio")]
        public AudioID dashSound = AudioID.SlasherAttack;
        public AudioID slamSound = AudioID.ShoebillAreaImpact;
        public AudioID deathSound = AudioID.SlasherDeath;

        private void OnValidate()
        {
            roamPause.y = Mathf.Max(roamPause.y, roamPause.x);
            avoidancePriorityRange.x = Mathf.Clamp(avoidancePriorityRange.x, 0, 99);
            avoidancePriorityRange.y = Mathf.Clamp(avoidancePriorityRange.y, avoidancePriorityRange.x, 99);

            if (slamStages == null) return;
            foreach (SlamStage stage in slamStages)
            {
                if (stage != null) stage.maxDamage = Mathf.Max(stage.maxDamage, stage.minDamage);
            }
        }
    }

    [Serializable]
    public class SlamStage
    {
        [Min(0.1f)] public float radius = 2.5f;

        [Tooltip("Time the circle shows before it hits.")]
        [Min(0.1f)] public float telegraphDuration = 0.8f;

        [Min(0f)] public float minDamage = 6f;
        [Min(0f)] public float maxDamage = 8f;

        [Tooltip("Seconds the players hit can't move, attack or interact. 0 = no stun.")]
        [Min(0f)] public float stunDuration;

        public SlamStage() { }

        public SlamStage(float radius, float telegraphDuration, float minDamage, float maxDamage, float stunDuration = 0f)
        {
            this.radius = radius;
            this.telegraphDuration = telegraphDuration;
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
            this.stunDuration = stunDuration;
        }
    }
}
