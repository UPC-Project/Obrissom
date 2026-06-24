using Unity.Netcode;
using UnityEngine;

public class DPSCombat : NetworkBehaviour
{
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void FireMagicProjectileServerRpc(Vector3 spawnPosition, Vector3 directionToTarget, int minDamage, int maxDamage, float speed, float lifeTime)
    {
        ChannelMagicProjectile.ExecuteOnServer(gameObject, spawnPosition, directionToTarget, minDamage, maxDamage, speed, lifeTime);
    }
}
