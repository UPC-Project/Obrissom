using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PlayerInventory = Obrissom.Player.Inventory.Inventory;

/// UI panel showing active quest progress.
namespace Obrissom.UI
{
    public class QuestProgressUI : MonoBehaviour
    {
        public static QuestProgressUI Instance { get; private set; }
        [Header("UI References")]
        [SerializeField] private Transform _container;

        [Tooltip("Prefab for a quest entry (must contain a TMP_Text component).")]
        [SerializeField] private GameObject _questEntryPrefab;

        // string = quest id
        private Dictionary<string, GameObject> _activeQuests = new();

        private PlayerQuestTracker _tracker;
        private PlayerInventory _playerInventory;

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

        /// Binds this UI to a player's quest tracker and inventory, then refreshes.
        public void Bind(PlayerQuestTracker tracker, PlayerInventory inventory)
        {
            if (_tracker != null)
            {
                // Unsuscribe to prevent errors
                _tracker.OnQuestsChanged -= RefreshUI;
            }

            _tracker = tracker;
            _playerInventory = inventory;

            if (_tracker != null)
            {
                _tracker.OnQuestsChanged += RefreshUI;
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            if (_tracker != null)
            {
                _tracker.OnQuestsChanged -= RefreshUI;
            }
        }

        /// Show active quest progress in real time
        private void RefreshUI()
        {
            //if (_tracker == null || _tracker.ActiveQuests == null || _questEntryPrefab == null) return;
            if (_tracker == null || _tracker.ActiveQuests == null) return;
            StringBuilder progress = new();
            foreach (QuestInstance quest in _tracker.ActiveQuests)
            {
                if (quest.template.objective.type == QuestObjectiveType.Kill)
                {
                    for (int j = 0; j < quest.template.objective.enemyTargets.Length; j++)
                    {
                        int current = quest.GetKillProgress(j);
                        progress.AppendLine($"\n{quest.template.objective.enemyTargets[0].enemy.name} {current}/{quest.template.objective.enemyTargets[j].amount} - status: {quest.status}");
                    }
                }
            }

            Debug.Log(progress.ToString());



            // TODO

            //// Clear existing entries
            //foreach (Transform child in _container)
            //{
            //    Destroy(child.gameObject);
            //}

            //foreach (QuestInstance quest in _tracker.ActiveQuests)
            //{
            //    GameObject entry = Instantiate(_questEntryPrefab, _container);
            //    TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>();
            //    // TODO
            //}
        }
    }
}