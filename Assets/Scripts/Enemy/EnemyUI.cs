using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : NetworkBehaviour
{
    [SerializeField] Image _healthBarImage;
    [SerializeField] GameObject _healthBar;

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateHealthUIRpc(float amount, float maxHealth)
    {
        _healthBarImage.fillAmount = amount / maxHealth;
    }

    public void SetHealthBarActive(bool isActive)
    {
        _healthBar.SetActive(isActive);
    }
}
