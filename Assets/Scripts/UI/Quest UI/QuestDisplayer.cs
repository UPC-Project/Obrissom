using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

/// Handles quest info display.
namespace Obrissom.UI
{
    public class QuestDisplayer : MonoBehaviour
    {
        public static QuestDisplayer Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject _QuestModal;
        [SerializeField] private TextMeshProUGUI _questTitle;
        [SerializeField] private TextMeshProUGUI _questDescription;
        [SerializeField] private TextMeshProUGUI _reward;
        [SerializeField] private TextMeshProUGUI _requirements;
        [SerializeField] private Button _acceptQuestButton;
        [SerializeField] private Button _completeQuestButton;
        [SerializeField] private GameObject _lockedQuestButton;

        private Action _onAcceptCallback;
        private Action _onCompleteCallback;

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

            if (_acceptQuestButton != null) _acceptQuestButton.onClick.AddListener(OnAcceptClicked);
            if (_completeQuestButton != null) _completeQuestButton.onClick.AddListener(OnCompleteClicked);
        }

        // UI
        public void CloseQuestDisplayer()
        {
            _QuestModal.SetActive(false);
            _onAcceptCallback = null;
            _onCompleteCallback = null;
        }

        public void ShowQuestOffer(QuestTemplate template, Action onAccept)
        {
            _onAcceptCallback = onAccept;

            _questTitle.text = template.title;
            _questDescription.text = template.offerDescription;
            _reward.text = BuildRewardsString(template);
            _requirements.text = "";
            _acceptQuestButton.gameObject.SetActive(true);
            _lockedQuestButton.SetActive(false);
            _completeQuestButton.gameObject.SetActive(false);

            _QuestModal.SetActive(true);
        }

        public void ShowQuestCompletion(QuestTemplate template, Action onComplete)
        {
            _onCompleteCallback = onComplete;

            _questTitle.text = template.title + " [Completed]";
            _questDescription.text = template.completedDescription;
            _reward.text = BuildRewardsString(template);
            _requirements.text = "";
            _acceptQuestButton.gameObject.SetActive(false);
            _lockedQuestButton.SetActive(false);
            _completeQuestButton.gameObject.SetActive(true);

            _QuestModal.SetActive(true);
        }

        /// Shows quest info with missing requirements (level, prerequisite quests).
        public void ShowQuestLocked(QuestTemplate template, int playerLevel, List<string> missingQuestNames)
        {
            _questTitle.text = template.title + " [Locked]";
            _questDescription.text = template.offerDescription;
            _reward.text = BuildRewardsString(template);
            _requirements.text = BuildRequirementsString(template);
            _acceptQuestButton.gameObject.SetActive(false);
            _lockedQuestButton.SetActive(true);
            _completeQuestButton.gameObject.SetActive(false);

            _QuestModal.SetActive(true);
        }

        private void OnAcceptClicked()
        {
            _onAcceptCallback?.Invoke();
            CloseQuestDisplayer();
        }

        private void OnCompleteClicked()
        {
            _onCompleteCallback?.Invoke();
            CloseQuestDisplayer();
        }

        private string BuildRewardsString(QuestTemplate template)
        {
            StringBuilder sb = new StringBuilder();

            if (template.reward == null) return "";

            if (template.reward.experienceReward > 0)
            {
                sb.AppendLine($"<color=green>{template.reward.experienceReward} XP</color>");
            }

            if (template.reward.items.Count() >0)
            {
                sb.AppendLine($"\nItems:");
                foreach (ItemReward reward in template.reward.items)
                {
                    if (reward.item != null)
                    {
                        sb.AppendLine($"\n{reward.amount}x \"{reward.item.itemName}\"");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        private string BuildRequirementsString(QuestTemplate template)
        {
            StringBuilder sb = new();

            sb.AppendLine($"To accept this quest, you should reach level {template.requiredLevel}");
            if (template.requiredQuests.Count() != 0)
            {
                sb.AppendLine(" and complete the following quests:");

                foreach (QuestTemplate qt in template.requiredQuests)
                {
                    sb.AppendLine($"\n{qt.title}");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
