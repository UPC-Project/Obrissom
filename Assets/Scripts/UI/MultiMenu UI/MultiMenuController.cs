using Obrissom.Player;
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
        [SerializeField] private MenuPanel _inventory;
        [SerializeField] private MenuPanel _crafting;
        [SerializeField] private MenuPanel _quests;
        [SerializeField] private MenuPanel _stats;
        [SerializeField] private MenuPanel _skills;

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
        private MenuType? _currentMenu = null;
        private Dictionary<MenuType, (MenuPanel panel, Button button)> _menus;
        private int _totalMenus;
        enum MenuType { Inventory, Quests, Crafting, Stats, Skills }
        #endregion

        private void Awake()
        {
            _totalMenus = Enum.GetValues(typeof(MenuType)).Length;

            _menus = new Dictionary<MenuType, (MenuPanel, Button)>
            {
                { MenuType.Inventory, (_inventory, _inventoryButton) },
                { MenuType.Quests,    (_quests,    _questsButton)    },
                { MenuType.Crafting,  (_crafting,  _craftingButton)  },
                { MenuType.Stats,     (_stats,     _statsButton)     },
                { MenuType.Skills,    (_skills,    _skillsButton)    },
            };
        }

        private void Start()
        {
            foreach (var entry in _menus)
            {
                entry.Value.button.onClick.AddListener(() => Toggle(entry.Key));
            }

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
            Toggle(MenuType.Inventory);
        }

        public void OnOpenQuests(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            Toggle(MenuType.Quests);
        }

        public void OnOpenCrafting(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            Toggle(MenuType.Crafting);
        }

        public void OnOpenStats(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            Toggle(MenuType.Stats);
        }

        public void OnOpenSkills(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            Toggle(MenuType.Skills);
        }
        #endregion

        private void Toggle(MenuType menu)
        {
            OpenOrClose(menu);
            var entry = _menus[menu];
            entry.panel.SetMenuState(!entry.panel.IsOpen);
        }

        public void NextMenu()
        {
            int nextIndex = ((int)_currentMenu + 1) % _totalMenus;
            Toggle((MenuType)nextIndex);
        }

        public void PreviousMenu()
        {
            int prevIndex = ((int)_currentMenu - 1 + _totalMenus) % _totalMenus;
            Toggle((MenuType)prevIndex);
        }

        private void OpenOrClose(MenuType menu)
        {
            var entry = _menus[menu];

            if (!_multiMenu.activeSelf)
            {
                _multiMenu.SetActive(true);
                _currentMenu = menu;
                entry.button.image.color = Color.grey;
                SetButtonsText();
            }
            else if (_currentMenu != menu)
            {
                if (_currentMenu is MenuType prev && _menus.ContainsKey(prev))
                {
                    _menus[prev].button.image.color = Color.white;
                    _menus[prev].panel.SetMenuState(false);
                }
                entry.button.image.color = Color.grey;
                _currentMenu = menu;
                SetButtonsText();
            }
            else
            {
                entry.button.image.color = Color.white;
                _multiMenu.SetActive(false);
                _currentMenu = null;
            }
        }

        private void SetButtonsText()
        {
            int newPrev = ((int)_currentMenu - 1 + _totalMenus) % _totalMenus;
            int newNext = ((int)_currentMenu + 1) % _totalMenus;
            _prevText.text = "< " + ((MenuType)newPrev).ToString();
            _nextText.text = ((MenuType)newNext).ToString() + " >";
        }

    }
}

