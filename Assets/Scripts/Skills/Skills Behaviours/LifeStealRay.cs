using Obrissom.Enemy;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Life_Steal_Ray")]
public class LifeStealRay : SkillBehaviour
{
    [SerializeField] private float _range = 10f;
    [SerializeField] private float _tickInterval = 1f;
    [SerializeField] private float _healPerTick = 1f;

    private EnemyBase _targetEnemy;
    private float _tickTimer;
    private PlayerCombat _playerCombat;
    private NetworkObject _netObj;

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition) { }

    public override bool OnChannelStart(GameObject caster, Skill skillData)
    {
        _targetEnemy = FindClosestEnemy(caster.transform.position);
        if (_targetEnemy == null) return false;

        _playerCombat = caster.GetComponent<PlayerCombat>();
        _netObj = caster.GetComponent<NetworkObject>();
        _tickTimer = 0f;

        return true;
    }

    public override bool OnChannelUpdate(GameObject caster, Skill skillData)
    {
        if (_targetEnemy == null || !_targetEnemy.IsSpawned || _targetEnemy.IsDead)
            return false;

        float distance = Vector3.Distance(caster.transform.position, _targetEnemy.transform.position);
        if (distance > _range) return false; // enemy too far away

        // Lock onto target
        Vector3 direction = _targetEnemy.transform.position - caster.transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            caster.transform.rotation = Quaternion.LookRotation(direction);
        }

        // Life steal tick
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;

            var (damage, isCritic) = _playerCombat.CalculateMagicDamage(skillData.minEffectValue, skillData.maxEffectValue);
            _targetEnemy.TakeDamageRpc(damage, EffectType.MagicDamage, false, _targetEnemy.transform.position, _netObj);

            _playerCombat.HealRpc(_healPerTick, false);
        }

        return true;
    }

    public override void OnChannelEnd(GameObject caster, Skill skillData)
    {
        _targetEnemy = null;
        _tickTimer = 0f;
    }

    private EnemyBase FindClosestEnemy(Vector3 origin)
    {
        Collider[] hits = Physics.OverlapSphere(origin, _range);
        EnemyBase closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (!hit.transform.root.CompareTag("Enemy")) continue;

            EnemyBase enemy = hit.transform.root.GetComponent<EnemyBase>();
            if (enemy == null || !enemy.IsSpawned || enemy.IsDead) continue;

            float distance = Vector3.Distance(origin, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }
}

