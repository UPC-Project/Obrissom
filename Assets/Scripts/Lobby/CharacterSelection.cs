using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CharacterSelection : MonoBehaviour
{
    public static CharacterSelection Instance { get; private set; }

    [Serializable]
    public class ClassData
    {
        public PlayerClass ClassType;
        public GameObject Model;
        public Button Button;
        [TextArea] public string Description;
        public LevelUpRewards Rewards;
        [HideInInspector] public Sprite NormalSprite;
    }

    [Serializable]
    public class SkillSlotUI
    {
        public Image Icon;
        public TMP_Text Description;
    }

    [Header("UI Components")]
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private GameObject _clientPanel;
    [SerializeField] private TMP_Text _descriptionClassText;
    [SerializeField] private List<SkillSlotUI> _skillSlots;
    [SerializeField] private GameObject _classDescription;
    [SerializeField] private GameObject _classSkillsInfo;
    [SerializeField] private TMP_InputField _playerNameInputField;
    [SerializeField] private TMP_InputField _roomCodeInputField;
    [SerializeField] private GameObject _connectingText;
    [SerializeField] private TMP_Text _errorText;

    [Header("Classes")]
    [SerializeField] private List<ClassData> _classes;

    private Dictionary<PlayerClass, ClassData> _classesMap;
    private PlayerClass _currentClass;

    public PlayerClass CurrentClass => _currentClass;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _classesMap = new Dictionary<PlayerClass, ClassData>();
        foreach (var classData in _classes)
        {
            classData.NormalSprite = classData.Button.image.sprite;
            _classesMap[classData.ClassType] = classData;

            PlayerClass classType = classData.ClassType;
            classData.Button.onClick.AddListener(() => SelectClass(classType));
        }
    }

    public void SelectClass(PlayerClass targetClass)
    {
        _currentClass = targetClass;

        foreach (var data in _classes)
        {
            bool isSelected = data.ClassType == targetClass;
            data.Model.SetActive(isSelected);
            data.Button.image.sprite = isSelected
                ? data.Button.spriteState.selectedSprite
                : data.NormalSprite;
        }

        ClassData selected = _classesMap[targetClass];
        _descriptionClassText.text = selected.Description;

        List<Skill> skills = GetSkills(selected.Rewards);
        for (int i = 0; i < _skillSlots.Count; i++)
        {
            _skillSlots[i].Icon.sprite = skills[i].skillImage;
            _skillSlots[i].Description.text = skills[i].description;
        }
    }

    private List<Skill> GetSkills(LevelUpRewards rewards)
    {
        List<Skill> skills = new List<Skill> { rewards.basicSkill };
        foreach (var reward in rewards.rewards)
        {
            if (reward.newSkill != null)
                skills.Add(reward.newSkill);
        }
        return skills;
    }

    public void HideCharactersModels()
    {
        foreach (var data in _classes)
            data.Model.SetActive(false);
    }

    public void SetCharacterSelection(bool isHost)
    {
        _hostPanel.SetActive(isHost);
        _clientPanel.SetActive(!isHost);
        _connectingText.SetActive(false);
        _errorText.gameObject.SetActive(false);
        SetClassButtonsInteractable(true);

        PlayerClass randomClass = _classes[Random.Range(0, _classes.Count)].ClassType;
        SelectClass(randomClass);
    }

    // toggle within description and skills information
    public void ToggleClassInfo(bool isDescription)
    {
        _classDescription.SetActive(isDescription);
        _classSkillsInfo.SetActive(!isDescription);
    }

    public async void StartGame(bool isHost)
    {
        string playerName = _playerNameInputField.text.Trim();
        string roomCode = _roomCodeInputField.text.Trim();
        if (playerName == "" || playerName == null)
        {
            _errorText.gameObject.SetActive(true);
            _errorText.text = "* Please choose a name.";
            return;
        }
        if ((roomCode == "" || roomCode == null) && !isHost)
        {
            _errorText.gameObject.SetActive(true);
            _errorText.text = "* Room code is needed.";
            return;
        }

        _hostPanel.SetActive(false);
        _clientPanel.SetActive(false);
        _errorText.gameObject.SetActive(false);
        _connectingText.SetActive(true);
        SetClassButtonsInteractable(false);

        bool success = isHost
            ? await LobbyNetworking.Instance.CreateLobby(playerName, _currentClass) != null
            : await LobbyNetworking.Instance.JoinLobby(_roomCodeInputField.text.Trim(), playerName, _currentClass);

        if (!success)
        {
            _connectingText.SetActive(false);
            _hostPanel.SetActive(isHost);
            _clientPanel.SetActive(!isHost);
            SetClassButtonsInteractable(true);
            _errorText.text = "* " + LobbyNetworking.Instance.LastError;
            _errorText.gameObject.SetActive(true);
        }
    }

    private void SetClassButtonsInteractable(bool interactable)
    {
        foreach (var data in _classes)
            data.Button.interactable = interactable;
    }
}
