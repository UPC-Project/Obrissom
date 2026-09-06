using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest")]
public class QuestTemplate : ScriptableObject
{
    [Tooltip("Unique identifier for this quest (used for network sync and lookups).\nUse convention: T-XX for test quests, P-XX for primary quests and S-XX for secondary quests.")]
    public string questId;

    public string title;
    [TextArea] public string offerDescription;
    [TextArea] public string completedDescription;
    public QuestType type;

    [Tooltip("Objectives and rewards are shared between all players")]
    public bool isShared;

    [Tooltip("Required player level to accept this quest"), Range(1, 5)]
    public int requiredLevel = 1;

    [Tooltip("Quests that must be completed before accepting this one")]
    public QuestTemplate[] requiredQuests;

    [Header("NPCs")]
    [Tooltip("NPC Id (from NPCInteractable)")]
    public string questGiver;

    [Tooltip("NPC Id (from NPCInteractable)")]
    public string questReceiver;

    [Header("Objective & Rewards")]
    public QuestObjective objective;
    public QuestReward reward;
}
