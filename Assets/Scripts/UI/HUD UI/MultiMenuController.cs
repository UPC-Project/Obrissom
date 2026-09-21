using Obrissom.Player;
using Obrissom.Player.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    // Opens/closes: inventory, stats, crafting, quests, skills
    public class MultiMenuController : MonoBehaviour, PlayerInput.IUIMultiMenuActions
    {
        [Header("Panels")]
        [SerializeField] private GameObject _multiMenu;
        [SerializeField] private InventoryManager _inventory;

        [Header("Buttons")]
        [SerializeField] private Button _inventoryButton; // I
        [SerializeField] private Button _questsButton; // O
        [SerializeField] private Button _craftingButton; // J
        [SerializeField] private Button _statsButton; // L
        [SerializeField] private Button _skillsButton; // k
        private PlayerInput _playerInput;
        private Menus? _currentPanel = null;
        private Dictionary<Menus, Button> _buttons;
        private Dictionary<Menus, Action> _close;

        enum Menus { Inventory, Quests, Crafting, Stats, Skills }

        private void Awake()
        {
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
                { Menus.Inventory, CloseInventory },
                { Menus.Quests, CloseQuests },
                { Menus.Crafting, CloseCrafting },
                { Menus.Stats, CloseStats },
                { Menus.Skills, CloseSkills },
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

        #region closePanel
        private void CloseInventory()
        {
            _inventory.SetInventoryState(false);
        }

        private void CloseQuests()
        {
        }

        private void CloseCrafting()
        {
        }
        private void CloseStats()
        {
        }
        private void CloseSkills()
        {
        }
        #endregion

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

            Debug.Log($"Open/Close Inventory {_inventory.isInventoryOpen}");
        }

        private void ToggleQuests()
        {
            OpenOrClose(Menus.Quests);
            Debug.Log("Open/Close Quests");
        }

        private void ToggleCrafting()
        {
            OpenOrClose(Menus.Crafting);
            Debug.Log("Open/Close Crafting");
        }

        private void ToggleStats()
        {
            OpenOrClose(Menus.Stats);
            Debug.Log("Open/Close Stats");
        }

        private void ToggleSkills()
        {
            OpenOrClose(Menus.Skills);
            Debug.Log("Open/Close Skills");
        }
        #endregion

        private void OpenOrClose(Menus menu)
        {
            if (!_multiMenu.activeSelf)
            {
                _multiMenu.SetActive(true);
                _currentPanel = menu;
                _buttons[menu].image.color = Color.grey;
            }
            else if (_currentPanel != menu)
            {
                if (_currentPanel is Menus panel && _buttons.ContainsKey(panel))
                {
                    _buttons[panel].image.color = Color.white;
                    _close[panel]();
                }
                _buttons[menu].image.color = Color.grey;
                _currentPanel = menu;
            }
            else
            {
                _buttons[menu].image.color = Color.white;
                _multiMenu.SetActive(false);
                _currentPanel = null;
            }
        }

    }
}
