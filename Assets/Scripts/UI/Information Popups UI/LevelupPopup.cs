using TMPro;
using UnityEngine;

namespace Obrissom.UI
{
    public class LevelupPopup : InformationPopUp
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        public static LevelupPopup Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public void ShowLevelupPopup(string level)
        {
            _levelText.text = "Level " + level;
            StartCoroutine(Show());
        }
    }
}
