using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.Player.Inventory
{
    // May delete later, ui functionallity was moved to InventoryMenu, now only holds this testing fun
    public class InventoryManager : MonoBehaviour
    {
        [Header("Connections")]
        public Inventory inventory;

        [Header("Test Items (Press G)")]
        public Item testItem1;
        public int amount1 = 1;
        public Item testItem2;
        public int amount2 = 1;
        public Item testItem3;
        public int amount3 = 1;
        public Item testItem4;
        public int amount4 = 1;
        public void BindInventory(Inventory playerInventory)
        {
            inventory = playerInventory;
        }

        void Update()
        {
            // Add items for testing
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame && inventory != null)
            {
                inventory.AddItem(testItem1, amount1);
                inventory.AddItem(testItem2, amount2);
                inventory.AddItem(testItem3, amount3);
                inventory.AddItem(testItem4, amount4);
            }
        }
    }
}
