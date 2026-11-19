using Obrissom.Player;
using Obrissom.Player.Inventory;
using TMPro;
using UnityEngine;

namespace Obrissom.UI
{
    public class PlayerUIManager : MonoBehaviour
    {
        public static PlayerUIManager Instance { get; private set; }

        [SerializeField] private LevelAndXPUI _levelAndXPUI;
        [SerializeField] private MultiMenuController _playerMenu;
        [SerializeField] private SkillCooldownUI _skillCooldownUI;
        [SerializeField] private HealthAndResourceUI _healthAndResourceUI;
        [SerializeField] private InventoryMenu _inventoryMenu;
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private CraftingMenu _craftingMenu;
        [SerializeField] private SkillsUI _skillsUI;
        [SerializeField] private SkillsMenu _skillsMenu;

        [Header("Room Code")]
        [SerializeField] private GameObject _roomCodePanel;
        [SerializeField] private TMP_Text _roomCodeText;
        [SerializeField] private TMP_Text _roomCodeTextSettings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SetRoomCode(LobbyNetworking.Instance.JoinCode);
        }

        public void SetRoomCode(string code)
        {
            _roomCodeText.text = "Room code: " + code;
            _roomCodeTextSettings.text = "Room code: " + code;
            _roomCodePanel.SetActive(true);
        }

        public void CloseRoomCodePanel()
        {
            _roomCodePanel.SetActive(false);
        }

        public LevelAndXPUI GetLevelAndXPUI() => _levelAndXPUI;
        public MultiMenuController GetPlayerMenu() => _playerMenu;
        public HealthAndResourceUI GetHealthAndResourceUI() => _healthAndResourceUI;
        public SkillsUI GetSkillsUI() => _skillsUI;

        public void RegisterPlayer(PlayerSkills playerSkills)
        {
            _skillCooldownUI.SetPlayerSkills(playerSkills);
        }

        public void RegisterPlayerInventory(ItemDropper itemDropper)
        {
            Inventory playerInventory = itemDropper.GetComponent<Inventory>();
            PlayerXP playerXp = itemDropper.GetComponent<PlayerXP>();
            _inventoryMenu.BindLocalPlayer(playerInventory, itemDropper);
            _inventoryManager.BindInventory(playerInventory);
            _craftingMenu.BindInventory(playerInventory);
            _skillsMenu.BindPlayerXP(playerXp);
        }
    }
}