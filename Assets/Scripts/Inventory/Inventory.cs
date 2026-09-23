using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// This class manages the item data for a player or container.
    /// It handles adding, moving, and removing items without touching the UI.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int _inventorySize = 20;

        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        public IReadOnlyList<InventorySlot> Slots => _slots;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            // TODO: save/restore inventory information
            for (int i = 0; i < _inventorySize; i++)
            {
                _slots.Add(new InventorySlot(null, 0));
            }
        }

        /// <summary>
        /// Adds an item to the inventory. 
        /// Logic: Fill existing stacks first, then use empty slots.
        /// </summary>
        public bool AddItem(Item item, int amount)
        {
            // Step 1: If the item can stack, look for slots with the same item
            if (item.isStackable)
            {
                foreach (var slot in _slots)
                {
                    if (!slot.CanAccept(item)) continue;

                    // If same item and has space in stack
                    if (slot.item == item && slot.quantity < item.maxStackSize)
                    {
                        int spaceLeft = item.maxStackSize - slot.quantity;
                        int toAdd = Mathf.Min(amount, spaceLeft);

                        slot.AddQuantity(toAdd);
                        amount -= toAdd;

                        OnInventoryChanged?.Invoke(); 

                        if (amount <= 0) return true; // All items added
                    }
                }
            }

            // Step 2: If items are left, look for the first empty slot
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty && slot.CanAccept(item))
                {
                    slot.item = item;
                    slot.quantity = amount;

                    OnInventoryChanged?.Invoke(); 
                    return true;
                }
            }

            return false; // Inventory is full
        }

        /// <summary>
        /// Swaps or moves items between two slot indexes.
        /// </summary>
        public void MoveItem(int sourceIndex, int destinationIndex)
        {
            if (sourceIndex == destinationIndex) return;

            InventorySlot source = _slots[sourceIndex];
            InventorySlot destination = _slots[destinationIndex];

            // Check if the slots can accept the items (for equipment restrictions)
            if (source.item != null && !destination.CanAccept(source.item)) return;
            if (destination.item != null && !source.CanAccept(destination.item)) return;

            // Swap values
            Item tempItem = destination.item;
            int tempQuantity = destination.quantity;

            destination.item = source.item;
            destination.quantity = source.quantity;

            source.item = tempItem;
            source.quantity = tempQuantity;

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Removes an item from a slot. Used for dropping or consuming items.
        /// amountToDrop = -1 means remove the entire stack.
        /// </summary>
        public bool RemoveItemAt(int slotIndex, out Item removedItem, out int removedQuantity, int amountToDrop = -1)
        {
            removedItem = null;
            removedQuantity = 0;

            // Index safety check
            if (slotIndex < 0 || slotIndex >= _slots.Count) return false;

            InventorySlot slot = _slots[slotIndex];
            if (slot.IsEmpty) return false;

            removedItem = slot.item;

            // Decide if we remove all or just a part of the stack
            bool dropAll = amountToDrop == -1 || !slot.item.isStackable || amountToDrop >= slot.quantity;

            if (dropAll)
            {
                removedQuantity = slot.quantity;
                slot.item = null;
                slot.quantity = 0;
            }
            else
            {
                removedQuantity = amountToDrop;
                slot.RemoveQuantity(amountToDrop);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Returns the total quantity of a specific item across all slots.
        /// </summary>
        public int GetItemCount(Item item)
        {
            int count = 0;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && slot.item == item)
                    count += slot.quantity;
            }
            return count;
        }

        /// <summary>
        /// Removes a specific amount of an item from the inventory across all slots.
        /// Returns true if the full amount was successfully removed.
        /// </summary>
        public bool RemoveItem(Item item, int amount)
        {
            if (GetItemCount(item) < amount) return false;

            for (int i = 0; i < _slots.Count && amount > 0; i++)
            {
                var slot = _slots[i];
                if (slot.IsEmpty || slot.item != item) continue;

                int toRemove = Mathf.Min(amount, slot.quantity);
                slot.RemoveQuantity(toRemove);
                amount -= toRemove;

                if (slot.quantity <= 0)
                {
                    slot.item = null;
                    slot.quantity = 0;
                }
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Force the UI to refresh without changing data.
        /// </summary>
        public void TriggerInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
