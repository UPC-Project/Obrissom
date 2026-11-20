using UnityEngine;
using Obrissom.UI;

// Manages quest offering and completion for an NPC.
[RequireComponent(typeof(NPCInteractable))]
public class QuestGiver : MonoBehaviour
{
    [SerializeField, Tooltip("Quests this NPC can offer to players.")] private QuestTemplate[] _offeredQuests;

    /// Marker to show over this NPC for the given player. Same order of checks as HandleInteraction.
    public QuestMarkerType GetMarker(PlayerQuestTracker player)
    {
        string npcId = GetComponent<NPCInteractable>().NpcId;
        bool waitingHere = false;

        foreach (QuestInstance quest in player.ActiveQuests)
        {
            QuestTemplate template = quest.template;
            if (template == null) continue;

            // Talking to this NPC finishes a talk quest
            QuestObjective objective = template.objective;
            bool talkHere = quest.status == QuestStatus.InProgress && objective != null
                && objective.type == QuestObjectiveType.Talk && objective.targetNPC == npcId && !quest.IsTalkCompleted();

            if (talkHere || (template.questReceiver == npcId && quest.status == QuestStatus.ReadyToDeliver))
                return QuestMarkerType.ReadyToDeliver;

            if (template.questReceiver == npcId && quest.status == QuestStatus.InProgress)
                waitingHere = true;
        }

        // An unfinished quest here blocks new offers, like in HandleInteraction
        if (waitingHere) return QuestMarkerType.InProgress;

        if (_offeredQuests != null)
        {
            foreach (QuestTemplate template in _offeredQuests)
            {
                if (template == null || player.HasCompletedQuest(template) || player.HasActiveQuest(template)) continue;
                if (player.CheckRequirements(template).isMet) return QuestMarkerType.Available;
            }
        }

        return QuestMarkerType.None;
    }

    /// Returns true if quest UI was shown, false if no relevant quest to display.
    public bool HandleInteraction(PlayerQuestTracker player)
    {
        string npcId = gameObject.GetComponent<NPCInteractable>().NpcId;

        // Check if the player has a quest ready to deliver at this NPC
        foreach (QuestInstance quest in player.ActiveQuests)
        {
            if (quest.template.questReceiver == npcId && quest.status == QuestStatus.ReadyToDeliver)
            {
                QuestDisplayer.Instance?.ShowQuestCompletion(quest.template, () =>
                {
                    player.CompleteQuest(quest.template);
                });
                return true;
            }
        }

        // Also check InProgress quests whose receiver is this NPC
        foreach (QuestInstance quest in player.ActiveQuests)
        {
            if (quest.template.questReceiver == npcId && quest.status == QuestStatus.InProgress)
            {
                // Player has this quest but it's not done yet
                return false;
            }
        }

        // Find the first quest to offer
        if (_offeredQuests != null)
        {
            QuestTemplate firstLockedQuest = null;
            RequirementCheckResult firstLockedResult = default;

            foreach (QuestTemplate template in _offeredQuests)
            {
                if (template == null) continue;

                if (player.HasCompletedQuest(template)) continue;
                if (player.HasActiveQuest(template)) continue;

                /// Check requirements
                RequirementCheckResult result = player.CheckRequirements(template);
                if (result.isMet)
                {
                    QuestDisplayer.Instance?.ShowQuestOffer(template, () =>
                    {
                        player.AcceptQuest(template);
                    });
                    return true;
                }
                else if (firstLockedQuest == null)
                {
                    firstLockedQuest = template;
                    firstLockedResult = result;
                }
            }

            // If we found no available quests but found a locked one, show the locked one
            if (firstLockedQuest != null)
            {
                QuestDisplayer.Instance?.ShowQuestLocked(
                    firstLockedQuest,
                    firstLockedResult.playerLevel,
                    firstLockedResult.missingQuestNames
                );
                return true;
            }
        }

        // All quests completed or none available
        return false;
    }
}
