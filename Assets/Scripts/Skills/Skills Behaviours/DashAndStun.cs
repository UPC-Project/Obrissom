using Obrissom.Enemy;
using Obrissom.Player;
using Obrissom.UI;
using System.Collections;
using System.Collections.Generic;
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

    // Cached at runtime
    private GameObject _dashArea;
    private Transform _dashAreaTransform;

    private GameObject GetDashArea()
    {
        if (_dashArea == null)
        {
            _dashArea = TankUIManager.Instance.GetDashArea();
            _dashAreaTransform = _dashArea.transform;
        }
        return _dashArea;
    }

    public override void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GetDashArea().SetActive(true);
    }

    public override void OnHoldUpdate(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        Transform casterTransform = caster.transform;
        float dashDistance = CalculateDashDistance(casterTransform.position, casterTransform.forward);

        _dashAreaTransform.localScale = new Vector3(_dashWidth, dashDistance, 1f);
    }

    public override void OnCancel(GameObject caster, Skill skillData)
    {
        GetDashArea().SetActive(false);
    }

    public override bool OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GetDashArea().SetActive(false);

        Transform casterTransform = caster.transform;
        Vector3 origin = casterTransform.position;
        Vector3 forward = casterTransform.forward;

        float dashDistance = CalculateDashDistance(origin, forward);
        Vector3 dashDestination = origin + forward * dashDistance;

        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        playerCombat.StartCoroutine(DashAndStunCoroutine(caster, playerCombat, dashDestination, forward));
        return true;
    }

    private IEnumerator DashAndStunCoroutine(GameObject caster, PlayerCombat playerCombat, Vector3 destination, Vector3 dashDirection)
    {
        PlayerLocomotionInput locomotionInput = caster.GetComponent<PlayerLocomotionInput>();
        PlayerCombatInput combatInput = caster.GetComponent<PlayerCombatInput>();
        PlayerController playerController = caster.GetComponent<PlayerController>();
        CharacterController characterController = caster.GetComponent<CharacterController>();

        locomotionInput.enabled = false;
        combatInput.enabled = false;
        playerController.enabled = false;

        playerCombat.SetInvulnerable(true);

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
                _enemyLayer
            );

            foreach (Collider hit in hits)
            {
                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy != null && !enemy.IsDead && !draggedEnemies.Contains(enemy))
                {
                    draggedEnemies.Add(enemy);
                    NavMeshAgentToggle(enemy, false);
                    enemy.ApplyDebuffRpc(DebuffType.Stun, _stunDuration);
                }
            }

            Vector3 rightDirection = Vector3.Cross(Vector3.up, dashDirection).normalized;

            for (int i = 0; i < draggedEnemies.Count; i++)
            {
                EnemyBase enemy = draggedEnemies[i];
                if (enemy.IsDead) continue;

                // -- grid for dragged enemies
                int row = i / 3;
                int col = i % 3;

                float lateralMultiplier = col == 0 ? 0f : (col == 1 ? 1f : -1f);
                
                float lateralSpacing = _dashWidth * 0.35f; 
                float depthSpacing = 1.2f;
                
                float baseDepth = 1.5f; 

                float lateralOffset = lateralMultiplier * lateralSpacing;
                float depthOffset = baseDepth + (row * depthSpacing);

                Vector3 enemyTarget = caster.transform.position + (dashDirection * depthOffset) + (rightDirection * lateralOffset);
                // -- grid

                enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, enemyTarget, step * 2.5f);
            }

            yield return null;
        }

        // Reenable NavMeshAgent
        foreach (EnemyBase enemy in draggedEnemies)
        {
            if (enemy.IsDead) continue;
            NavMeshAgentToggle(enemy, true);
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

    private void NavMeshAgentToggle(EnemyBase enemy, bool enabled)
    {
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null) return;

        if (enabled)
        {
            agent.enabled = true;
            agent.Warp(enemy.transform.position);
        }
        else
        {
            agent.enabled = false;
        }
    }

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition) { }
}

