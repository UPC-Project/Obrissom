using UnityEngine;
using UnityEngine.UI;


public class PlayerMenu : MonoBehaviour
{
    [Header("Components")]
    private Obrissom.Player.Inventory.InventoryManager _inventory;

    [Header("UI")]
    [SerializeField] private Button _inventoryButton;

    private void Start()
    {
    }
    public void OpenCloseInventory()
    {
        // TODO: herarchy will be transferred here
        //_inventory.SetInventoryState(!_inventory.isInventoryOpen);
        //_inventoryButton.image.color = _inventory.isInventoryOpen ? Color.grey : Color.white;
        Debug.Log("Open/Close Inventory");
    }

    public void OpenCloseStats()
    {
        Debug.Log("Open/Close Stats");
    }

    public void OpenCloseSkills()
    {
        Debug.Log("Open/Close Skills");
    }

    public void OpenCloseQuests()
    {
        Debug.Log("Open/Close Quests");
    }
}

