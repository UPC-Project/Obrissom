using Obrissom.Enemy;
using Obrissom.Player;
using Obrissom.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using PlayerInventory = Obrissom.Player.Inventory.Inventory;

public struct RequirementCheckResult
{
    public bool isMet;
    public int playerLevel;
    public List<string> missingQuestNames;
}

/// Manages active and completed quests.
/// Tracks objective progress and delegates shared quest updates to QuestManager.
public class PlayerQuestTracker : MonoBehaviour
{
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private PlayerXP _playerXP;

    private List<QuestInstance> _activeQuests = new List<QuestInstance>();
    private List<QuestTemplate> _completedQuests = new List<QuestTemplate>();

    public IReadOnlyList<QuestInstance> ActiveQuests => _activeQuests;
    public IReadOnlyList<QuestTemplate> CompletedQuests => _completedQuests;

    /// Fired whenever quest data changes (accept, progress, completion).
    public event Action OnQuestsChanged;

    private void Awake()
    {
        if (_inventory == null) _inventory = GetComponent<PlayerInventory>();
        if (_playerXP == null) _playerXP = GetComponent<PlayerXP>();
    }

    void Start()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.RegisterLocalTracker(this);

        if (QuestProgressUI.Instance != null)
            QuestProgressUI.Instance.Bind(this, _inventory);
    }

    // QUEST LIFECYCLE

    /// Accepts a quest and creates a QuestInstance.
    /// If the quest is shared, registers it with QuestManager.
    public void AcceptQuest(QuestTemplate template)
    {
        if (template == null) return;
        if (HasActiveQuest(template) || HasCompletedQuest(template)) return;

        QuestInstance instance = new QuestInstance(template);
        _activeQuests.Add(instance);

        // Register shared quests with the server
        if (template.isShared && QuestManager.Instance != null)
        {
            QuestManager.Instance.RegisterSharedQuest(template.questId, this);
        }

        OnQuestsChanged?.Invoke();
    }

    /// Completes a quest: applies rewards, deducts Collect items, and moves to completed list.
    public void CompleteQuest(QuestTemplate template)
    {
        QuestInstance instance = GetQuestInstance(template);
        if (instance == null) return;
        if (!instance.IsObjectiveComplete(_inventory)) return;

        // Deduct collected items from inventory
        DeductCollectItems(template);

        // Apply rewards
        ApplyRewards(template);

        // Move quest to completed
        instance.status = QuestStatus.Completed;
        _activeQuests.Remove(instance);
        _completedQuests.Add(template);

        OnQuestsChanged?.Invoke();
    }

    // PROGRESS REPORTING

    /// Called when this player kills an enemy.
    public void ReportKill(EnemyStats enemyStats)
    {
        if (enemyStats == null) return;

        bool changed = false;

        foreach (QuestInstance quest in _activeQuests)
        {
            if (quest.status != QuestStatus.InProgress) continue;

            QuestObjective objective = quest.template.objective;
            if (objective == null || objective.type != QuestObjectiveType.Kill || objective.enemyTargets == null) continue;

            for (int j = 0; j < objective.enemyTargets.Length; j++)
            {
                if (objective.enemyTargets[j].enemy == enemyStats)
                {
                    if (quest.template.isShared && QuestManager.Instance != null)
                    {
                        QuestManager.Instance.ReportKillProgress(quest.template.questId, j, 1);
                    }
                    else
                    {
                        quest.AddKillProgress(j, 1);
                        changed = true;
                    }
                }
            }

            quest.CheckAndUpdateStatus(_inventory);
        }

        if (changed) OnQuestsChanged?.Invoke();
    }

    /// Called when this player interacts with an NPC. Updates Talk objectives.
    public void ReportTalk(QuestGiver npc, string npcId)
    {
        if (npc == null) return;

        bool changed = false;

        foreach (QuestInstance quest in _activeQuests)
        {
            if (quest.status != QuestStatus.InProgress) continue;

            QuestObjective obj = quest.template.objective;
            if (obj == null || obj.type != QuestObjectiveType.Talk) continue;
            if (obj.targetNPC != npcId) continue;

            if (!quest.IsTalkCompleted())
            {
                if (quest.template.isShared && QuestManager.Instance != null)
                {
                    QuestManager.Instance.ReportTalkProgress(quest.template.questId);
                }
                else
                {
                    quest.SetTalkCompleted();
                    changed = true;
                }
            }

            quest.CheckAndUpdateStatus(_inventory);
        }

        if (changed) OnQuestsChanged?.Invoke();
    }

    /// Called when the player's inventory changes. Re-evaluates Collect objectives.
    public void RefreshCollectProgress()
    {
        bool changed = false;

        foreach (QuestInstance quest in _activeQuests)
        {
            if (quest.status != QuestStatus.InProgress) continue;

            QuestStatus previousStatus = quest.status;
            quest.CheckAndUpdateStatus(_inventory);

            if (quest.status != previousStatus) changed = true;
        }

        if (changed) OnQuestsChanged?.Invoke();
    }

    // SHARED QUEST SYNC

    /// Called by QuestManager to sync kill progress from the server.
    public void SyncKillProgress(string questId, int enemyTargetIndex, int currentProgress)
    {
        QuestInstance quest = GetQuestInstanceById(questId);
        if (quest == null) return;

        quest.SetKillProgress(enemyTargetIndex, currentProgress);
        quest.CheckAndUpdateStatus(_inventory);
        OnQuestsChanged?.Invoke();
    }

    /// Called by QuestManager to sync talk progress from the server.
    public void SyncTalkProgress(string questId)
    {
        QuestInstance quest = GetQuestInstanceById(questId);
        if (quest == null) return;

        quest.SetTalkCompleted();
        quest.CheckAndUpdateStatus(_inventory);
        OnQuestsChanged?.Invoke();
    }

    // QUERY METHODS

    public bool HasActiveQuest(QuestTemplate template)
    {
        foreach (QuestInstance quest in _activeQuests)
        {
            if (quest.template == template) return true;
        }
        return false;
    }

    public bool HasCompletedQuest(QuestTemplate template)
    {
        return _completedQuests.Contains(template);
    }

    public QuestInstance GetQuestInstance(QuestTemplate template)
    {
        foreach (QuestInstance quest in _activeQuests)
        {
            if (quest.template == template) return quest;
        }
        return null;
    }

    public QuestInstance GetQuestInstanceById(string questId)
    {
        foreach (QuestInstance quest in _activeQuests)
        {
            if (quest.template.questId == questId) return quest;
        }
        return null;
    }

    public QuestStatus GetQuestStatus(QuestTemplate template)
    {
        if (HasCompletedQuest(template)) return QuestStatus.Completed;

        QuestInstance instance = GetQuestInstance(template);
        if (instance != null) return instance.status;

        if (!CheckRequirements(template).isMet) return QuestStatus.Locked;

        return QuestStatus.Available;
    }

    /// Checks if the player meets level and prerequisite quest requirements.
    /// Returns a result with details about what's missing.
    public RequirementCheckResult CheckRequirements(QuestTemplate template)
    {
        RequirementCheckResult result = new RequirementCheckResult
        {
            isMet = true,
            playerLevel = _playerXP != null ? _playerXP.currentLevel : 1,
            missingQuestNames = new List<string>()
        };

        // Check level requirement
        if (result.playerLevel < template.requiredLevel)
        {
            result.isMet = false;
        }

        // Check prerequisite quests
        if (template.requiredQuests != null)
        {
            foreach (QuestTemplate required in template.requiredQuests)
            {
                if (required != null && !HasCompletedQuest(required))
                {
                    result.isMet = false;
                    result.missingQuestNames.Add(required.title);
                }
            }
        }

        return result;
    }

    // PRIVATE HELPERS

    private void ApplyRewards(QuestTemplate template)
    {
        if (template.reward == null) return;

        // xp
        if (template.reward.experienceReward > 0 && _playerXP != null)
        {
            _playerXP.GainXP(template.reward.experienceReward);
        }

        // items
        if (template.reward.items != null && _inventory != null)
        {
            foreach (ItemReward itemReward in template.reward.items)
            {
                if (itemReward.item != null)
                {
                    _inventory.AddItem(itemReward.item, itemReward.amount);
                }
            }
        }
    }

    private void DeductCollectItems(QuestTemplate template)
    {
        if (template.objective == null || _inventory == null || template.objective.type != QuestObjectiveType.Collect || template.objective.itemTargets == null) return;

        foreach (ItemTarget target in template.objective.itemTargets)
        {
            if (target.item == null) continue;
            RemoveItemFromInventory(target.item, target.amount);
        }

    }

    /// Removes a specific amount of an item from the inventory across all slots.
    private void RemoveItemFromInventory(Item item, int amountToRemove)
    {
        if (_inventory == null || amountToRemove <= 0) return;

        for (int i = 0; i < _inventory.Slots.Count && amountToRemove > 0; i++)
        {
            var slot = _inventory.Slots[i];
            if (slot.IsEmpty || slot.item != item) continue;

            int removeFromSlot = Mathf.Min(amountToRemove, slot.quantity);
            _inventory.RemoveItemAt(i, out _, out _, removeFromSlot);
            amountToRemove -= removeFromSlot;
        }
    }
}
