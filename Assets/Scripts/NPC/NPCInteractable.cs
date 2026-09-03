using UnityEngine;

/// Generic interaction script for all NPCs.
/// Handles player interaction, reports Talk quest progress, and delegates to QuestGiver if the NPC has quests.
/// Falls back to generic dialogue via NPCDialogueUI otherwise.
public class NPCInteractable : MonoBehaviour
{
    [Header("NPC Info")]
    [SerializeField] private string _npcId;
    [SerializeField] private string _npcName;
    [SerializeField, TextArea] private string[] _dialogueLines;


    private QuestGiver _questGiver;

    private void Awake()
    {
        _questGiver = GetComponent<QuestGiver>();
    }

    public string NpcId => _npcId;
    public string NpcName => _npcName;


    public void OnInteract(PlayerQuestTracker player)
    {
        if (_questGiver != null)
        {
            player.ReportTalk(_questGiver, _npcId);
        }

        if (_questGiver != null)
        {
            bool handled = _questGiver.HandleInteraction(player);
            if (handled) return;
        }

        if (_dialogueLines != null && _dialogueLines.Length > 0)
        {
            NPCDialogueUI.Instance?.ShowDialogue(_npcName, _dialogueLines);
        }
    }
}
