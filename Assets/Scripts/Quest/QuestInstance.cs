using UnityEngine;

/// Runtime representation of an active quest.
/// Tracks kill and talk progress. Collect progress is derived from inventory.
public class QuestInstance
{
    public QuestTemplate template;
    public QuestStatus status;

    private int[] _killProgress;

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

    public int GetCollectProgress(int itemTargetIndex, Obrissom.Player.Inventory.Inventory inventory)
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

        return Mathf.Min(count, target.amount);
    }

    // COMPLETION CHECKS

    public bool IsObjectiveComplete(Obrissom.Player.Inventory.Inventory inventory)
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
        if (status == QuestStatus.InProgress && IsObjectiveComplete(inventory))
        {
            status = QuestStatus.ReadyToDeliver;
        }
    }
}
