using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : NetworkBehaviour
{
    [SerializeField] Image _HealthBar;

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateHealthUIRpc(float amount, float maxHealth)
    {
        _HealthBar.fillAmount = amount / maxHealth;
    }
}
