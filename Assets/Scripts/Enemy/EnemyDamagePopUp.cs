using Unity.Netcode;
using UnityEngine;

public class EnemyDamagePopUp : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void ShowPopUpClientRpc(string damageAmount, DamageType damageType, bool isCritic, Vector3 hitPos)
    {
        Debug.Log("ShowPopUpClientRpc");
        if (damageType == DamageType.MagicDamage)
        {
        Debug.Log("MagicDamagePopUpPool");
            MagicDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount, isCritic);
        }
        else
        {
        Debug.Log("PhyiscDamagePopUpPool");
            PhyiscDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount, isCritic);
        }
    }
}
