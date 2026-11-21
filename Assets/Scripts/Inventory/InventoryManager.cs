using UnityEngine;

namespace Obrissom.Player.Inventory
{
    // Keeps the local player's inventory for the UI. The UI itself lives in InventoryMenu.
    public class InventoryManager : MonoBehaviour
    {
        [Header("Connections")]
        public Inventory inventory;

        public void BindInventory(Inventory playerInventory)
        {
            inventory = playerInventory;
        }
    }
}
