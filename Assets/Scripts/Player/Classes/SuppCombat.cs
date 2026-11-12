using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Bridge between networking and skill behaviours.
/// Specific for Supp class.
/// </summary>
namespace Obrissom.Player
{
    public class SuppCombat : NetworkBehaviour
    {
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ChannelHealDamageRayServerRpc(Vector3 spawnPosition, Vector3 directionToTarget, int minDamage, int maxDamage, float speed, float lifeTime)
        {
            ChannelHealOrDamage.ExecuteOnServer(gameObject, spawnPosition, directionToTarget, minDamage, maxDamage, speed, lifeTime);
        }
    }
}
