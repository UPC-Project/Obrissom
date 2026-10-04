using TMPro;
using UnityEngine;

namespace Obrissom.UI
{
    public class QuestCompletePopup : InformationPopUp
    {
        [SerializeField] private TextMeshProUGUI _questNameText;
        public static QuestCompletePopup Instance { get; private set; }
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
        public void ShowQuestCompletePopup(string questName)
        {
            _questNameText.text = questName;
            StartCoroutine(Show());
        }
    }
}
