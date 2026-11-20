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

        [Header("Class Icon")]
        [Tooltip("Por debajo de este porcentaje de salud se muestra el icono de dañado.")]
        [SerializeField, Range(0f, 1f)] private float _damagedThreshold = 0.35f;

        [Header("Health Colors")]
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _mediumColor = Color.yellow;
        [SerializeField] private Color _lowColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color _criticalColor = Color.red;

        private Sprite _healthyIcon;
        private Sprite _damagedIcon;

        public void Setup(Sprite healthyIcon, Sprite damagedIcon, string playerName, float health, float maxHealth)
        {
            _healthyIcon = healthyIcon;
            _damagedIcon = damagedIcon;

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
            UpdateClassIcon(percent);
        }

        private void UpdateClassIcon(float percent)
        {
            if (_classIcon == null) return;

            bool damaged = percent < _damagedThreshold;

            _classIcon.sprite = (damaged && _damagedIcon != null) ? _damagedIcon : _healthyIcon;
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