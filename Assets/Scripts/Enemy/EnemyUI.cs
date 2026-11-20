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
        if (_healthBarImage != null) _healthBarImage.fillAmount = amount / maxHealth;
    }

    public void SetHealthBarActive(bool isActive)
    {
        if (_healthBar != null) _healthBar.SetActive(isActive);
    }
}
