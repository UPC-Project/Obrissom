using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    public class LevelAndXPUI : MonoBehaviour
    {
        [SerializeField] Image _xpBar;
        [SerializeField] TextMeshProUGUI _xpAmountText;
        [SerializeField] TextMeshProUGUI _xpLevelText;

        public void UpdateXP(float amount, float max, int level)
        {
            if (level == LevelUpRequirements.MAX_LEVEL)
            {
                _xpBar.fillAmount = 100f;
                _xpAmountText.text = $"max level";
                _xpLevelText.text = $"Lvl 5";
            }
            else
            {
                _xpBar.fillAmount = amount / max;
                _xpAmountText.text = $"{amount} / {max}";
                _xpLevelText.text = $"Lvl {level}";
            }
        }

    }
}
