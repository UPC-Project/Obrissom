using UnityEngine;
using TMPro;

public class NPCDialogueUI : MonoBehaviour
{
    public static NPCDialogueUI Instance { get; private set; }

    [Header("Dialogue Panel Components")]
    [SerializeField] private GameObject _container;
    [SerializeField] private TMP_Text _npcNameText;
    [SerializeField] private TMP_Text _dialogueText;

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

        Close();
    }


    public void ShowDialogue(string npcName, string[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        _npcNameText.text = npcName;
        int randomIndex = Random.Range(0, lines.Length);
        _dialogueText.text = lines[randomIndex];

        _container.SetActive(true);
    }

    public void Close() => _container.SetActive(false);
}
