using Unity.Netcode;
using UnityEngine;

public class TestEnemy : NetworkBehaviour
{
    public void TakeDamage(float damageAmount, EffectType damageType, bool isCritic, Vector3 hitPos)
    {
        if (!IsServer) return;
        GetComponent<EnemyDamagePopUp>().ShowPopUpClientRpc(damageAmount.ToString(), damageType, isCritic, hitPos);
    }
}
