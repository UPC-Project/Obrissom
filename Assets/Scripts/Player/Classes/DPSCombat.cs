using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Bridge between networking and skill behaviours.
/// Specific for DPS class.
/// </summary>
namespace Obrissom.Player
{
    public class DPSCombat : NetworkBehaviour
    {
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ChannelMagicProjectileServerRpc(Vector3 spawnPosition, Vector3 directionToTarget, int minDamage, int maxDamage, float speed, float lifeTime)
        {
            ChannelMagicProjectile.ExecuteOnServer(gameObject, spawnPosition, directionToTarget, minDamage, maxDamage, speed, lifeTime);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void PhysicInstantFrontServerRpc(int minDamage, int maxDamage, float range, float angle, EffectType effect, int minDamagePerSecond, int maxDamagePerSecond, float damagePerSecondTime, EffectType damagePerSecondType)
        {
            // Manually had to pass all skill data needed, since RPC functions only acept serializable types (maybe could use INetworkSerializable)
            InstantFront.ExecuteOnServer(gameObject, minDamage, maxDamage, range, angle, effect, minDamagePerSecond, maxDamagePerSecond, damagePerSecondTime, damagePerSecondType);
        }
    }
}
