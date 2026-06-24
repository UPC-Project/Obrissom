using Unity.Netcode;
using UnityEngine;

public class EnemyDamagePopUp : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void ShowPopUpClientRpc(string damageAmount, DamageType damageType, bool isCritic, Vector3 hitPos)
    {
        if (damageType == DamageType.MagicDamage)
        {
            MagicDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount, isCritic);
        }
        else
        {
            PhyiscDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount, isCritic);
        }
    }
}
