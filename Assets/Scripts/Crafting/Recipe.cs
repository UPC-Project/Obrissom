using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject
{
    public string title;
    [TextArea] public string description;
    public RecipeItemsType[] items;
    public Item result;
}
