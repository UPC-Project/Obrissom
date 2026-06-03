using Obrissom.Player;
using Obrissom.Player.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Obrissom.UI
{
    public class PlayerUIManager : MonoBehaviour
    {
        public static PlayerUIManager Instance { get; private set; }

        [SerializeField] private LevelAndXPUI _levelAndXPUI;
        [SerializeField] private PlayerMenu _playerMenu;
        [SerializeField] private SkillCooldownUI _skillCooldownUI;
        [SerializeField] private HealthAndResourceUI _healthAndResourceUI;
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private GameObject _crosshair;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public LevelAndXPUI GetLevelAndXPUI() => _levelAndXPUI;
        public PlayerMenu GetPlayerMenu() => _playerMenu;
        public HealthAndResourceUI GetHealthAndResourceUI() => _healthAndResourceUI;
        public GameObject GetCrosshair() => _crosshair;

        public void RegisterPlayer(PlayerSkills playerSkills)
        {
            _skillCooldownUI.SetPlayerSkills(playerSkills);
        }

        public void RegisterPlayerInventory(ItemDropper itemDropper)
        {
            Inventory playerInventory = itemDropper.GetComponent<Inventory>();
            _inventoryManager.BindLocalPlayer(playerInventory, itemDropper);
        }
    }
}