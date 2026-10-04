using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Slasher-specific configuration. Assign one asset per difficulty variant.
    /// General stats (health, speed, damage range) live in EnemyStats.
    /// </summary>
    [CreateAssetMenu(fileName = "New SlasherConfig", menuName = "Obrissom/Enemy/SlasherConfig")]
    public class SlasherConfig : ScriptableObject
    {
        [Header("Combo")]
        [Tooltip("Number of hits per combo sequence.")]
        [Min(1)] public int comboHits = 2;

        [Tooltip("Delay between active frames of one hit and the windup of the next.")]
        [Min(0f)] public float delayBetweenHits = 0.2f;

        [Header("Attack Timing")]
        [Tooltip("Time between starting the attack and the hitbox going active. This is the player's reaction window.")]
        [Min(0f)] public float windupDuration = 0.4f;

        [Tooltip("How long the hitbox stays active per hit.")]
        [Min(0f)] public float activeFramesDuration = 0.15f;

        [Tooltip("Recovery time after the last combo hit. The slasher is vulnerable during this window.")]
        [Min(0f)] public float recoveryDuration = 0.5f;

        [Header("Sweep")]
        [Tooltip("Full angle of the slash arc in degrees. 120 = 60 degrees on each side of forward.")]
        [Range(0f, 360f)] public float sweepAngle = 120f;

        [Header("Lunge")]
        [Tooltip("Lunge fires if the player is beyond this fraction of attack range. 0.6 = lunge when player is farther than 60% of attack range.")]
        [Range(0f, 1f)] public float lungeTriggerRatio = 0.6f;

        [Tooltip("NavMesh speed during the lunge.")]
        [Min(0f)] public float lungeSpeed = 12f;

        [Tooltip("How long the lunge movement lasts in seconds.")]
        [Min(0f)] public float lungeDuration = 0.2f;

        [Header("Enrage")]
        [Tooltip("Slasher enrages when HP drops below this fraction. 0.3 = below 30% HP.")]
        [Range(0f, 1f)] public float enrageHealthThreshold = 0.3f;

        [Tooltip("Combo hits per sequence while enraged.")]
        [Min(1)] public int enragedComboHits = 3;

        [Tooltip("Multiplier applied to windupDuration when enraged. 0.7 = 30% faster windup.")]
        [Range(0f, 1f)] public float enrageWindupMultiplier = 0.7f;

        [Header("Regeneration")]
        [Tooltip("Seconds without taking damage before regeneration starts.")]
        [Min(0f)] public float regenDelay = 3f;

        [Tooltip("HP regenerated per second as a fraction of max HP. 0.03 = 3% per second.")]
        [Range(0f, 1f)] public float regenRate = 0.03f;

        [Tooltip("How much HP is recovered relative to HP at regen start. 0.2 = regens up to 20% on top of HP at regen start.")]
        [Range(0f, 1f)] public float regenCap = 0.2f;

        [Header("Retreat")]
        [Tooltip("Total raw damage received within the time window that triggers a retreat.")]
        [Min(1f)] public float retreatDamageThreshold = 30f;

        [Tooltip("Time window in seconds to accumulate damage toward the retreat threshold.")]
        [Min(0f)] public float retreatDamageWindow = 2f;

        [Tooltip("Delay before moving after the retreat triggers. Lets the hit stagger play out.")]
        [Min(0f)] public float retreatDelay = 0.3f;

        [Tooltip("How far the slasher moves away from the player when retreating.")]
        [Min(0f)] public float retreatDistance = 4f;

        [Tooltip("NavMesh speed during retreat.")]
        [Min(0f)] public float retreatSpeed = 5f;

        [Tooltip("Maximum time spent retreating before re-engaging.")]
        [Min(0f)] public float retreatDuration = 1.2f;

        [Tooltip("Invulnerability window after retreat ends, before re-engaging.")]
        [Min(0f)] public float postRetreatInvulnerabilityDuration = 1.5f;
    }
}
