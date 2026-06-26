using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [SerializeField] Image _HealthBar;

    public void UpdateHealthUI(float amount, float maxHealth)
    {
        _HealthBar.fillAmount = amount / maxHealth;
    }
}
