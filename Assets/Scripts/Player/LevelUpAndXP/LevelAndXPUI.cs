using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.Player
{
    public class LevelAndXPUI : MonoBehaviour
    {
        [SerializeField] Image xpBar;
        [SerializeField] TextMeshProUGUI xpAmountText;
        [SerializeField] TextMeshProUGUI xpLevelText;

        public void UpdateXP(float amount, float max, int level)
        {
            if (level == LevelUpRequirements.MAX_LEVEL)
            {
                xpBar.fillAmount = 100f;
                xpAmountText.text = $"max level";
                xpLevelText.text = $"Lvl 5";
            }
            else
            {
                xpBar.fillAmount = amount / max;
                xpAmountText.text = $"{amount} / {max}";
                xpLevelText.text = $"Lvl {level}";
            }
        }

    }
}
