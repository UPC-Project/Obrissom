using Unity.Netcode;
using UnityEngine;

public class EnemyDamagePopUp : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void ShowPopUpClientRpc(string damageAmount, EffectType damageType, bool isCritic, Vector3 hitPos)
    {
        if (damageType == EffectType.MagicDamage)
        {
            MagicDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount, isCritic);
        }
        else if (damageType == EffectType.PhysicDamage)
        {
            PhyiscDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount, isCritic);
        }
    }
}
