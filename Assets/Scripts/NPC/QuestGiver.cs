using UnityEngine;
using Obrissom.UI;

// Manages quest offering and completion for an NPC.
[RequireComponent(typeof(NPCInteractable))]
public class QuestGiver : MonoBehaviour
{
    [SerializeField, Tooltip("Quests this NPC can offer to players.")] private QuestTemplate[] _offeredQuests;

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
                else
                {
                    QuestDisplayer.Instance?.ShowQuestLocked(
                        template,
                        result.playerLevel,
                        result.missingQuestNames
                    );
                    return true;
                }
            }
        }

        // All quests completed or none available
        return false;
    }
}
