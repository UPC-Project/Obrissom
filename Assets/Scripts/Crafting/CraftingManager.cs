using Obrissom.Player.Inventory;

/// <summary>
/// Crafting logic
/// </summary>
public static class CraftingManager
{
    public static bool CanCraft(Recipe recipe, Inventory inventory)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (inventory.GetItemCount(ingredient.item) < ingredient.amount)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Executes the crafting: consumes ingredients and adds the result to inventory.
    /// Returns true if the craft was successful.
    /// </summary>
    public static bool Craft(Recipe recipe, Inventory inventory)
    {
        if (!CanCraft(recipe, inventory)) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;
            inventory.RemoveItem(ingredient.item, ingredient.amount);
        }

        return inventory.AddItem(recipe.result, recipe.resultAmount);
    }
}

