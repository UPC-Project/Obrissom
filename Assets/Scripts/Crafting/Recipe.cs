using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject
{
    // recipe uses item name and description
    [Header("Database")]
    public int recipeID;

    [Header("Ingredients")]
    public IngredientsType[] ingredients;

    [Header("Result")]
    public Item result;
    public int resultAmount = 1;
}

