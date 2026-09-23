using System.Collections.Generic;
using Obrissom.Database;
using Obrissom.Player.Inventory;
using UnityEngine;

public class CraftingMenu : MenuPanel
{
    [SerializeField] private GameObject _recipePanelPrefab;
    [SerializeField] private Transform _recipeListContainer;

    private Inventory _playerInventory;
    private List<RecipePanel> _recipePanels = new List<RecipePanel>();

    private void Start()
    {
        /// Instantiates a RecipePanel for each recipe in the database.
        var allRecipes = RecipeDatabase.Instance.GetAllRecipes();

        foreach (var recipe in allRecipes)
        {
            GameObject panelGO = Instantiate(_recipePanelPrefab, _recipeListContainer);
            RecipePanel panel = panelGO.GetComponent<RecipePanel>();
            panel.Initialize(recipe, OnCraftClicked);
            _recipePanels.Add(panel);
        }
    }

    public override void SetMenuState(bool state)
    {
        base.SetMenuState(state);
        if (state) RefreshAllPanels();
    }

    /// <summary>
    /// Called by PlayerUIManager when the local player spawns.
    /// Binds the player's inventory so recipe panels can query item counts.
    /// </summary>
    public void BindInventory(Inventory inventory)
    {
        if (_playerInventory != null)
            _playerInventory.OnInventoryChanged -= RefreshAllPanels;

        _playerInventory = inventory;

        if (_playerInventory != null)
        {
            _playerInventory.OnInventoryChanged += RefreshAllPanels;
            RefreshAllPanels();
        }
    }

    /// <summary>
    /// Callback invoked when the craft button is pressed on any recipe panel.
    /// </summary>
    private void OnCraftClicked(Recipe recipe)
    {
        if (_playerInventory == null) return;
        CraftingManager.Craft(recipe, _playerInventory);
        // RefreshAllPanels is triggered automatically via OnInventoryChanged
    }

    /// <summary>
    /// Updates all recipe panels with current inventory counts and button states.
    /// </summary>
    private void RefreshAllPanels()
    {
        foreach (var panel in _recipePanels)
            panel.Refresh(_playerInventory);
    }
}
