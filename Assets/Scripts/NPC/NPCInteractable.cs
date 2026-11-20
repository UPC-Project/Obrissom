using UnityEngine;

/// Generic interaction script for all NPCs.
/// Handles player interaction, reports Talk quest progress, and delegates to QuestGiver if the NPC has quests.
/// Falls back to generic dialogue via NPCDialogueUI otherwise.
public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("NPC Info")]
    [SerializeField] private string _npcId;
    [SerializeField] private string _npcName;
    [SerializeField, TextArea] private string[] _dialogueLines;
    [SerializeField] private GameObject _interactPrompt;
    [SerializeField] private Animator _animator;

    public Vector3 Position => transform.position;

    public void SetInteractPromptActive(bool active)
    {
        if (_interactPrompt != null)
            _interactPrompt.SetActive(active);
    }


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
            if(_animator != null)
            {
                _animator.SetTrigger("interact");
            }
            
            if (handled) return;
        }

        if (_dialogueLines != null && _dialogueLines.Length > 0)
        {
            NPCDialogueUI.Instance.ShowDialogue(_npcName, _dialogueLines);
        }
    }


    public void OnTriggerEnter(Collider other)
    {
        if (_animator != null)
        {
            _animator.SetTrigger("hello");
        }
    }
}
