using System;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// Defines the category of the slot. 
    /// 'All' is for standard inventory, 'Equipment' for specialized gear slots, 'Consumable' for quick-use slots.
    /// </summary>
    public enum SlotType { All, Equipment, Consumable }

    /// <summary>
    /// This class represents a single slot in an inventory or equipment panel.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public Item item;                              
        public int quantity;                            
        public SlotType slotType;

        /// <summary>
        /// Constructor to create a new slot.
        /// </summary>
        public InventorySlot(Item newItem, int amount, SlotType slotType = SlotType.All)
        {
            this.item = newItem;
            this.quantity = amount;
            this.slotType = slotType;
        }

        /// <summary>
        /// Validates if an item can be placed in this slot.
        /// Useful for restricting equipment slots (e.g., only weapons in weapon slots).
        /// </summary>
        public bool CanAccept(Item incomingItem)
        {
            // Standard inventory slots accept anything
            if (slotType == SlotType.All) return true;

            if (slotType == SlotType.Equipment)
                return incomingItem.itemType == Item.ItemType.Equippable;

            if (slotType == SlotType.Consumable)
                return incomingItem.itemType == Item.ItemType.Consumable;

            return false;
        }

        // Helper methods to modify the quantity
        public void AddQuantity(int amount) => quantity += amount;
        public void RemoveQuantity(int amount) => quantity -= amount;

        /// <summary>
        /// Returns true if the slot has no item or the quantity is zero.
        /// </summary>
        public bool IsEmpty => item == null || quantity <= 0;
    }
}
