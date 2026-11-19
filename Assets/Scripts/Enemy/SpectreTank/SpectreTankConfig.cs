using Obrissom.Audio;
using UnityEngine;

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

        [Header("NavMesh Agent")]
        [Min(0f)] public float acceleration = 8f;
        [Min(0f)] public float angularSpeed = 240f;
        [Min(0f)] public float stoppingDistance = 1.5f;

        [Header("Audio")]
        public AudioID dashSound = AudioID.SlasherAttack;
        public AudioID deathSound = AudioID.SlasherDeath;
    }
}
