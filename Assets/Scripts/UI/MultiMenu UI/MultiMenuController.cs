using Obrissom.Player;
using Obrissom.Player.Inventory;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    // Opens/closes: inventory, stats, crafting, quests, skills
    public class MultiMenuController : MonoBehaviour, PlayerInput.IUIMultiMenuActions
    {
        #region
        [Header("Panels")]
        [SerializeField] private GameObject _multiMenu;
        [SerializeField] private InventoryManager _inventory;
        [SerializeField] private CraftingMenu _crafting;
        [SerializeField] private QuestsMenu _quests;
        [SerializeField] private StatsMenu _stats;
        [SerializeField] private SkillsMenu _skills;

        [Header("Panel Buttons")]
        [SerializeField] private Button _inventoryButton; // I
        [SerializeField] private Button _questsButton; // O
        [SerializeField] private Button _craftingButton; // J
        [SerializeField] private Button _statsButton; // L
        [SerializeField] private Button _skillsButton; // K

        [Header("Carousel Buttons")]
        [SerializeField] private TextMeshProUGUI _prevText;
        [SerializeField] private TextMeshProUGUI _nextText;

        private PlayerInput _playerInput;
        private Menus? _currentMenu = null;
        private Dictionary<Menus, Button> _buttons;
        private Dictionary<Menus, Action> _close;
        private Dictionary<Menus, Action> _toggles;
        private int _totalMenus;
        enum Menus { Inventory, Quests, Crafting, Stats, Skills }
        #endregion

        private void Awake()
        {
            _totalMenus = Enum.GetValues(typeof(Menus)).Length;

            _buttons = new Dictionary<Menus, Button>()
            {
                { Menus.Inventory, _inventoryButton },
                { Menus.Quests, _questsButton },
                { Menus.Crafting, _craftingButton },
                { Menus.Stats, _statsButton },
                { Menus.Skills, _skillsButton },
            };

            _close = new Dictionary<Menus, Action>()
            {
                { Menus.Inventory, () => _inventory.SetInventoryState(false)},
                { Menus.Quests, () => _quests.SetQuestsMenuState(false) },
                { Menus.Crafting, () => _crafting.SetCraftingMenuState(false) },
                { Menus.Stats, () => _stats.SetStatsMenuState(false) },
                { Menus.Skills, () => _skills.SetSkillMenuState(false) },
            };

            _toggles = new Dictionary<Menus, Action>()
            {
                { Menus.Inventory, ToggleInventory},
                { Menus.Quests, ToggleQuests},
                { Menus.Crafting,ToggleCrafting },
                { Menus.Stats, ToggleStats },
                { Menus.Skills, ToggleSkills },
            };
        }

        private void Start()
        {
            _inventoryButton.onClick.AddListener(ToggleInventory);
            _questsButton.onClick.AddListener(ToggleQuests);
            _craftingButton.onClick.AddListener(ToggleCrafting);
            _statsButton.onClick.AddListener(ToggleStats);
            _skillsButton.onClick.AddListener(ToggleSkills);

            if (InputStateManager.Instance.PlayerInput != null)
                SetupInput(InputStateManager.Instance.PlayerInput);
            else
                InputStateManager.Instance.OnPlayerInputRegistered += SetupInput;
        }
        private void SetupInput(PlayerInput input)
        {
            _playerInput = input;
            _playerInput.UIMultiMenu.Enable();
            _playerInput.UIMultiMenu.SetCallbacks(this);
        }

        private void OnEnable()
        {
            if (_playerInput != null)
            {
                _playerInput.UIMultiMenu.Enable();
                _playerInput.UIMultiMenu.SetCallbacks(this);
            }
        }

        private void OnDisable()
        {
            _playerInput.UIMultiMenu.Disable();
            _playerInput.UIMultiMenu.RemoveCallbacks(this);
            InputStateManager.Instance.OnPlayerInputRegistered -= SetupInput;
        }

        #region keycall
        public void OnOpenInventory(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            ToggleInventory();
        }

        public void OnOpenQuests(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            ToggleQuests();
        }

        public void OnOpenCrafting(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            ToggleCrafting();
        }

        public void OnOpenStats(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            ToggleStats();
        }

        public void OnOpenSkills(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            ToggleSkills();
        }
        #endregion

        #region toggle
        private void ToggleInventory()
        {
            OpenOrClose(Menus.Inventory);
            _inventory.SetInventoryState(!_inventory.isInventoryOpen);

        }

        private void ToggleQuests()
        {
            OpenOrClose(Menus.Quests);
            _quests.SetQuestsMenuState(!_quests.isQuestsMenuOpen);
        }

        private void ToggleCrafting()
        {
            OpenOrClose(Menus.Crafting);
            _crafting.SetCraftingMenuState(!_crafting.isCraftingMenuOpen);

        }

        private void ToggleStats()
        {
            OpenOrClose(Menus.Stats);
            _stats.SetStatsMenuState(!_stats.isStatsMenuOpen);
        }

        private void ToggleSkills()
        {
            OpenOrClose(Menus.Skills);
            _skills.SetSkillMenuState(!_skills.isSkillMenuOpen);
        }
        #endregion

        public void NextMenu()
        {
            int nextIndex = ((int)_currentMenu + 1) % _totalMenus;
            _toggles[(Menus)nextIndex]();
            // set texts
            int newPrev = ((int)_currentMenu - 1 + _totalMenus) % _totalMenus;
            int newNext = ((int)_currentMenu + 1) % _totalMenus;
            _prevText.text = "< " + ((Menus)newPrev).ToString();
            _nextText.text = ((Menus)newNext).ToString() + " >";
        }

        public void PreviousMenu()
        {
            int prevIndex = ((int)_currentMenu - 1 + _totalMenus) % _totalMenus;
            _toggles[(Menus)prevIndex]();
            // set texts
            int newPrev = ((int)_currentMenu - 1 + _totalMenus) % _totalMenus;
            int newNext = ((int)_currentMenu + 1) % _totalMenus;
            _prevText.text = "< " + ((Menus)newPrev).ToString();
            _nextText.text = ((Menus)newNext).ToString() + " >";
        }

        private void OpenOrClose(Menus menu)
        {
            if (!_multiMenu.activeSelf)
            {
                _multiMenu.SetActive(true);
                _currentMenu = menu;
                _buttons[menu].image.color = Color.grey;
            }
            else if (_currentMenu != menu)
            {
                if (_currentMenu is Menus panel && _buttons.ContainsKey(panel))
                {
                    _buttons[panel].image.color = Color.white;
                    _close[panel]();
                }
                _buttons[menu].image.color = Color.grey;
                _currentMenu = menu;
            }
            else
            {
                _buttons[menu].image.color = Color.white;
                _multiMenu.SetActive(false);
                _currentMenu = null;
            }
        }

    }
}
