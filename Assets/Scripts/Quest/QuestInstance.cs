using UnityEngine;
using Obrissom.Player.Inventory;

/// Runtime representation of an active quest.
/// Tracks kill and talk progress. Collect progress is derived from inventory.
public class QuestInstance
{
    public QuestTemplate template;
    public QuestStatus status;

    private int[] _killProgress;
    private int[] _sharedCollectProgress;

    private bool _talkCompleted;

    public QuestInstance(QuestTemplate template)
    {
        this.template = template;
        this.status = QuestStatus.InProgress;

        QuestObjective obj = template.objective;
        if (obj != null && obj.type == QuestObjectiveType.Kill && obj.enemyTargets != null)
        {
            _killProgress = new int[obj.enemyTargets.Length];
        }
    }

    // KILL PROGRESS

    public void AddKillProgress(int enemyTargetIndex, int amount)
    {
        if (_killProgress == null || enemyTargetIndex < 0 || enemyTargetIndex >= _killProgress.Length) return;

        _killProgress[enemyTargetIndex] += amount;
        int required = template.objective.enemyTargets[enemyTargetIndex].amount;
        if (_killProgress[enemyTargetIndex] > required)
        {
            _killProgress[enemyTargetIndex] = required;
        }
    }

    public int GetKillProgress(int enemyTargetIndex)
    {
        if (_killProgress == null || enemyTargetIndex < 0 || enemyTargetIndex >= _killProgress.Length) return 0;
        return _killProgress[enemyTargetIndex];
    }

    public void SetKillProgress(int enemyTargetIndex, int value)
    {
        if (_killProgress == null || enemyTargetIndex < 0 || enemyTargetIndex >= _killProgress.Length) return;
        int required = template.objective.enemyTargets[enemyTargetIndex].amount;
        _killProgress[enemyTargetIndex] = Mathf.Min(value, required);
    }

    // TALK PROGRESS

    public void SetTalkCompleted()
    {
        _talkCompleted = true;
    }

    public bool IsTalkCompleted()
    {
        return _talkCompleted;
    }

    // COLLECT PROGRESS

    public void SetSharedCollectProgress(int itemTargetIndex, int value)
    {
        if (_sharedCollectProgress == null)
        {
            if (template.objective != null && template.objective.itemTargets != null)
                _sharedCollectProgress = new int[template.objective.itemTargets.Length];
            else return;
        }

        if (itemTargetIndex >= 0 && itemTargetIndex < _sharedCollectProgress.Length)
        {
            int required = template.objective.itemTargets[itemTargetIndex].amount;
            _sharedCollectProgress[itemTargetIndex] = Mathf.Min(value, required);
        }
    }

    public int GetCollectProgress(int itemTargetIndex, Inventory inventory)
    {
        if (template.isShared)
        {
            if (_sharedCollectProgress != null && itemTargetIndex >= 0 && itemTargetIndex < _sharedCollectProgress.Length)
            {
                return _sharedCollectProgress[itemTargetIndex];
            }
            return 0;
        }
        else
        {
            return GetLocalCollectProgress(itemTargetIndex, inventory);
        }
    }

    public int GetLocalCollectProgress(int itemTargetIndex, Inventory inventory)
    {
        QuestObjective obj = template.objective;
        if (obj == null || obj.itemTargets == null || itemTargetIndex < 0 || itemTargetIndex >= obj.itemTargets.Length)
            return 0;

        ItemTarget target = obj.itemTargets[itemTargetIndex];
        int count = 0;

        foreach (var slot in inventory.Slots)
        {
            if (!slot.IsEmpty && slot.item == target.item)
            {
                count += slot.quantity;
            }
        }

        return count; // Notice we don't cap this at target.amount here, so the server knows exactly how many they have for deduction!
    }

    // COMPLETION CHECKS

    public bool IsObjectiveComplete(Inventory inventory)
    {
        QuestObjective obj = template.objective;
        if (obj == null) return true;

        switch (obj.type)
        {
            case QuestObjectiveType.Kill:
                if (obj.enemyTargets == null) return true;
                for (int i = 0; i < obj.enemyTargets.Length; i++)
                {
                    if (GetKillProgress(i) < obj.enemyTargets[i].amount)
                        return false;
                }
                return true;

            case QuestObjectiveType.Collect:
                if (obj.itemTargets == null) return true;
                for (int i = 0; i < obj.itemTargets.Length; i++)
                {
                    if (GetCollectProgress(i, inventory) < obj.itemTargets[i].amount)
                        return false;
                }
                return true;

            case QuestObjectiveType.Talk:
                return IsTalkCompleted();

            default:
                return false;
        }
    }

    public void CheckAndUpdateStatus(Obrissom.Player.Inventory.Inventory inventory)
    {
        bool isComplete = IsObjectiveComplete(inventory);
        if (status == QuestStatus.InProgress && isComplete)
        {
            status = QuestStatus.ReadyToDeliver;
        }
        else if (status == QuestStatus.ReadyToDeliver && !isComplete)
        {
            status = QuestStatus.InProgress;
        }
    }
}
