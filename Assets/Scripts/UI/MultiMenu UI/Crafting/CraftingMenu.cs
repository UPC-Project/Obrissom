using UnityEngine;

public class CraftingMenu : MonoBehaviour
{
    public bool isCraftingMenuOpen = false;
    [SerializeField] private GameObject _craftingMenu;
    [SerializeField] private GameObject _recipePanel; // prefab

    private void Start()
    {
        // initialize database with all recipes available
    }

    public void SetCraftingMenuState(bool state)
    {
        _craftingMenu.SetActive(state);
        isCraftingMenuOpen = state;
    }
}
