using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    public class AllyHealthEntry : MonoBehaviour
    {
        [SerializeField] private Image _classIcon;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private Image _healthFill;

        [Header("Health Colors")]
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _mediumColor = Color.yellow;
        [SerializeField] private Color _lowColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color _criticalColor = Color.red;

        public void Setup(Sprite classIcon, string playerName, float health, float maxHealth)
        {
            if (_classIcon != null)
                _classIcon.sprite = classIcon;

            UpdateName(playerName);
            UpdateHealth(health, maxHealth);
        }

        public void UpdateName(string playerName)
        {
            _playerNameText.text = playerName;
        }

        public void UpdateHealth(float amount, float max)
        {
            float percent = max > 0f ? Mathf.Clamp01(amount / max) : 0f;
            _healthFill.fillAmount = percent;
            _healthFill.color = EvaluateHealthColor(percent);
        }

        private Color EvaluateHealthColor(float percent)
        {
            if (percent >= 0.5f)
                return _healthyColor;

            if (percent >= 0.3f)
                return Color.Lerp(_mediumColor, _healthyColor, Mathf.InverseLerp(0.3f, 0.5f, percent));

            if (percent >= 0.2f)
                return Color.Lerp(_lowColor, _mediumColor, Mathf.InverseLerp(0.2f, 0.3f, percent));

            return Color.Lerp(_criticalColor, _lowColor, Mathf.InverseLerp(0f, 0.2f, percent));
        }
    }
}

