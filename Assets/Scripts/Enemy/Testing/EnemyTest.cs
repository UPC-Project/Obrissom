using Obrissom.Enemy;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

public class EnemyTest : EnemyBase
{
    [Header("Test Attack Settings")]
    [SerializeField] private float _hitboxRadius = 1.5f;
    public override void PerformAttack()
    {
        if (_attackCooldownTimer > 0f) return;

        _attackCooldownTimer = _stats.attackCooldown;

        Collider[] hits = Physics.OverlapSphere(transform.position, _hitboxRadius, _playerLayer);

        foreach (var hit in hits)
        {
            PlayerCombat playerCombat = hit.GetComponentInParent<PlayerCombat>();
            if (playerCombat == null) continue;

            float damage = RollAttackDamage();
            playerCombat.TakeDamage(damage, _stats.damageType);
        }
    }

 
}