using System;
using System.Collections.Generic;
using Obrissom.Player.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipePanel : MonoBehaviour
{
    [Header("Recipe Info")]
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;

    [Header("Result")]
    [SerializeField] private Image _resultImage;
    [SerializeField] private TextMeshProUGUI _resultAmount;

    [Header("Ingredients")]
    [SerializeField] private Transform _ingredientsContainer;
    [SerializeField] private GameObject _ingredientSlotPrefab;

    [Header("Craft Button")]
    [SerializeField] private Button _craftButton;

    private Recipe _recipe;
    private Action<Recipe> _onCraftCallback;
    private List<IngredientSlotUI> _ingredientSlots = new List<IngredientSlotUI>();

    /// <summary>
    /// One-time setup called by CraftingMenu after instantiation.
    /// Populates static recipe data and spawns ingredient slots dynamically.
    /// </summary>
    public void Initialize(Recipe recipe, Action<Recipe> onCraftCallback)
    {
        _recipe = recipe;
        _onCraftCallback = onCraftCallback;

        _title.text = recipe.result.itemName;
        _description.text = recipe.result.description;
        _resultImage.sprite = recipe.result.image;
        _resultAmount.text = recipe.resultAmount > 1 ? $"x{recipe.resultAmount}" : "";

        // Spawn one IngredientSlotUI per ingredient
        foreach (var ingredient in recipe.ingredients)
        {
            GameObject slotGO = Instantiate(_ingredientSlotPrefab, _ingredientsContainer);
            IngredientSlotUI slotUI = slotGO.GetComponent<IngredientSlotUI>();
            slotUI.Setup(ingredient);
            _ingredientSlots.Add(slotUI);
        }

        _craftButton.onClick.AddListener(() => _onCraftCallback?.Invoke(_recipe));
    }

    /// <summary>
    /// Updates ingredient counts and craft button interactability
    /// based on the current inventory state.
    /// </summary>
    public void Refresh(Inventory inventory)
    {
        if (_recipe == null || inventory == null) return;

        bool canCraft = CraftingManager.CanCraft(_recipe, inventory);
        _craftButton.interactable = canCraft;

        foreach (var slotUI in _ingredientSlots)
            slotUI.UpdateCount(inventory);
    }
}

