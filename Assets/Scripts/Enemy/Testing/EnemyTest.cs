using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    public class EnemyTest : EnemyBase
    {
        [Header("Test Attack Settings")]
        [SerializeField] private float _hitboxRadius = 1.5f;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public override void PerformAttackRpc()
        {
            if (_attackCooldownTimer > 0f) return;

            _attackCooldownTimer = _stats.attackCooldown;

            Collider[] hits = Physics.OverlapSphere(transform.position, _hitboxRadius, _playerLayer); // TODO: change to Tigger enter/exit

            foreach (var hit in hits)
            {
                PlayerCombat playerCombat = hit.GetComponentInParent<PlayerCombat>();
                if (playerCombat == null) continue;

                float damage = RollAttackDamage();
                playerCombat.TakeDamage(damage, _stats.damageType);
            }
        }

    }
}