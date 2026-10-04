using Obrissom.Player.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for a single ingredient slot inside a RecipePanel.
/// Displays the ingredient image, name, and current/required count.
/// </summary>
public class IngredientSlotUI : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _countText;

    private IngredientsType _ingredient;

    public void Setup(IngredientsType ingredient)
    {
        _ingredient = ingredient;
        _image.sprite = ingredient.item.image;
    }

    /// <summary>
    /// Updates the count text and color based on current inventory.
    /// </summary>
    public void UpdateCount(Inventory inventory)
    {
        int playerHas = inventory.GetItemCount(_ingredient.item);
        _countText.text = $"{playerHas}/{_ingredient.amount}";
        _countText.color = playerHas >= _ingredient.amount ? Color.white : Color.red; // color may change when UI design is decided
    }
}

