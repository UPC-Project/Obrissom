using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthAndResourceUI : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] Image _healthFill;
    [SerializeField] TextMeshProUGUI _healthText;

    [Header("Resource")]
    [SerializeField] Image _resourceFill;
    [SerializeField] TextMeshProUGUI _resourceText;

    public void UpdateHealth(float amount, float max)
    {
        _healthFill.fillAmount = amount / max;
        _healthText.text = $"{Mathf.RoundToInt(amount)} / {Mathf.RoundToInt(max)}";
    }

    public void UpdateResource(float amount, float max)
    {
        _resourceFill.fillAmount = amount / max;
        _resourceText.text = $"{Mathf.RoundToInt(amount)} / {Mathf.RoundToInt(max)}";
    }
}
