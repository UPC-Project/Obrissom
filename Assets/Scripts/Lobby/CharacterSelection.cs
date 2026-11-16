using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }
    [Header("UI Components")]
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private GameObject _clientPanel;

    [Header("Classes models")]
    [SerializeField] private List<GameObject> _roles;
    [SerializeField] private List<Button> _roleButtons;

    private Sprite[] _normalSprites;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _normalSprites = new Sprite[_roleButtons.Count];
        for (int i = 0; i < _roleButtons.Count; i++)
            _normalSprites[i] = _roleButtons[i].image.sprite;
    }

    public void ShowRole(GameObject targetRole)
    {
        for (int i = 0; i < _roles.Count; i++)
        {
            bool isSelected = _roles[i] == targetRole;
            _roles[i].SetActive(isSelected);
            _roleButtons[i].image.sprite = isSelected
                ? _roleButtons[i].spriteState.selectedSprite
                : _normalSprites[i];
        }
    }

    public void HideCharactersModels()
    {
        foreach (var role in _roles)
            role.SetActive(false);
    }

    public void SetCharacterSelection(bool isHost)
    {
        if (isHost)
        {
            _hostPanel.SetActive(true);
            _clientPanel.SetActive(false);
        }
        else
        {
            _hostPanel.SetActive(false);
            _clientPanel.SetActive(true);
        }

        GameObject randomCharacter = _roles[Random.Range(0, _roles.Count)];
        ShowRole(randomCharacter);
    }
}
