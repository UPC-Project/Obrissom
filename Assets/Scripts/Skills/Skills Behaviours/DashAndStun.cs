using Obrissom.Enemy;
using Obrissom.Player;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Dash_and_Stun")]
public class DashAndStun : SkillBehaviour
{
    [Header("Dash")]
    [SerializeField] private float _maxDashDistance = 12f;
    [SerializeField] private float _dashSpeed = 25f;
    [SerializeField] private float _dashWidth = 3f;

    [Header("Stun")]
    [SerializeField] private float _stunDuration = 2.5f;

    [Header("Layers")]
    [Tooltip("blocks the dash (walls, trees, etc)")]
    [SerializeField] private LayerMask _obstacleLayer;

    [SerializeField] private LayerMask _enemyLayer;

    // Other players get a new lane length only when it changes this much
    private const float DashSyncStep = 0.25f;
    private float _sentDashDistance;

    private Vector3 DashScale(float dashDistance) => new Vector3(_dashWidth, dashDistance, 1f);

    public override void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        _sentDashDistance = CalculateDashDistance(caster.transform.position, caster.transform.forward);
        caster.GetComponent<PlayerCombat>().SetSkillArea(SkillArea.DashArea, true, DashScale(_sentDashDistance));
    }

    public override void OnHoldUpdate(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        float dashDistance = CalculateDashDistance(caster.transform.position, caster.transform.forward);

        if (Mathf.Abs(dashDistance - _sentDashDistance) >= DashSyncStep)
        {
            _sentDashDistance = dashDistance;
            playerCombat.SetSkillArea(SkillArea.DashArea, true, DashScale(dashDistance));
            return;
        }

        GameObject dashArea = playerCombat.GetSkillArea(SkillArea.DashArea);
        if (dashArea != null) dashArea.transform.localScale = DashScale(dashDistance);
    }

    public override void OnCancel(GameObject caster, Skill skillData)
    {
        caster.GetComponent<PlayerCombat>().SetSkillArea(SkillArea.DashArea, false);
    }

    public override bool OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        caster.GetComponent<PlayerCombat>().SetSkillArea(SkillArea.DashArea, false);

        Transform casterTransform = caster.transform;
        Vector3 origin = casterTransform.position;
        Vector3 forward = casterTransform.forward;

        float dashDistance = CalculateDashDistance(origin, forward);
        Vector3 dashDestination = origin + forward * dashDistance;

        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        playerCombat.StartCoroutine(DashAndStunCoroutine(caster, playerCombat, dashDestination, forward, skillData.minEffectValue, skillData.maxEffectValue, skillData.effectType));
        return true;
    }

    private IEnumerator DashAndStunCoroutine(GameObject caster, PlayerCombat playerCombat, Vector3 destination, Vector3 dashDirection, int minDamage, int maxDamage, EffectType effectType)
    {
        PlayerLocomotionInput locomotionInput = caster.GetComponent<PlayerLocomotionInput>();
        PlayerCombatInput combatInput = caster.GetComponent<PlayerCombatInput>();
        PlayerController playerController = caster.GetComponent<PlayerController>();
        CharacterController characterController = caster.GetComponent<CharacterController>();

        locomotionInput.enabled = false;
        combatInput.enabled = false;
        playerController.enabled = false;

        playerCombat.SetInvulnerable(true);

        // Enemies don't stop the dash, they get carried (obstacles already shortened it)
        LayerMask originalExclude = characterController.excludeLayers;
        characterController.excludeLayers = originalExclude | _enemyLayer;

        List<EnemyBase> draggedEnemies = new List<EnemyBase>();
        Vector3 boxHalfExtents = new Vector3(_dashWidth * 0.5f, 1f, 0.5f);

        float distanceToTravel = Vector3.Distance(caster.transform.position, destination);
        float traveled = 0f;

        while (traveled < distanceToTravel)
        {
            float step = _dashSpeed * Time.deltaTime;

            if (traveled + step > distanceToTravel) step = distanceToTravel - traveled;

            characterController.Move(dashDirection * step);
            traveled += step;

            Vector3 sweepCenter = caster.transform.position + dashDirection * 0.5f;
            Collider[] hits = Physics.OverlapBox(
                sweepCenter,
                boxHalfExtents,
                Quaternion.LookRotation(dashDirection),
                _enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy != null && !enemy.IsDeadSynced && !enemy.IsImmovable && !draggedEnemies.Contains(enemy))
                {
                    // -- grid for dragged enemies: 3 per row, in front of the caster
                    int i = draggedEnemies.Count;
                    int row = i / 3;
                    int col = i % 3;

                    float lateralMultiplier = col == 0 ? 0f : (col == 1 ? 1f : -1f);
                    float lateralOffset = lateralMultiplier * _dashWidth * 0.35f;
                    float depthOffset = 1.5f + row * 1.2f;
                    // -- grid

                    draggedEnemies.Add(enemy);
                    enemy.ApplyDebuffRpc(DebuffType.Stun, _stunDuration);

                    // The server moves the enemy along the rest of this dash
                    enemy.StartDragRpc(caster.transform.position, dashDirection, distanceToTravel - traveled, _dashSpeed,
                        lateralOffset, depthOffset, NetworkManager.Singleton.ServerTime.Time);

                    if (enemy.TryGetComponent<EnemyAnimation>(out var enemyAnim))
                    {
                        enemyAnim.SetStunned(true);
                    }
                }
            }

            yield return null;
        }

        characterController.excludeLayers = originalExclude;

        foreach (EnemyBase enemy in draggedEnemies)
        {
            if (enemy == null || enemy.IsDeadSynced) continue;
            // deal damage at the end
            if (minDamage > 0)
            {
                var (damage, isCritic) = effectType == EffectType.PhysicDamage
                ? playerCombat.CalculatePhysicalDamage(minDamage, maxDamage)
                : playerCombat.CalculateMagicDamage(minDamage, maxDamage);

                NetworkObject netObj = caster.GetComponent<NetworkObject>();
                enemy.TakeDamageRpc(damage, effectType, isCritic, enemy.transform.position, netObj);
            }
        }

        playerCombat.SetInvulnerable(false);
        locomotionInput.enabled = true;
        combatInput.enabled = true;
        playerController.enabled = true;
    }

    private float CalculateDashDistance(Vector3 origin, Vector3 forward)
    {
        float castRadius = _dashWidth * 0.4f;
        float distance = _maxDashDistance;

        Vector3 castOrigin = origin + Vector3.up * 0.5f;

        if (Physics.SphereCast(castOrigin, castRadius, forward, out RaycastHit hit, _maxDashDistance, _obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(hit.distance - 0.5f, 0.5f);
        }

        return distance;
    }

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition) { }
}

