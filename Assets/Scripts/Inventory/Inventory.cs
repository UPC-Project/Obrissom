using System;
using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] private int _inventorySize = 20; // Maximum number of slots

        // List of slots that hold the items
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        // Public access to slots (Read-only for safety)
        public IReadOnlyList<InventorySlot> Slots => _slots;

        // Event triggered every time an item is added, moved, or removed
        public event Action OnInventoryChanged;

        private void Awake()
        {
            // Initialize the inventory with empty slots
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

                        OnInventoryChanged?.Invoke(); // Refresh UI

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

                    OnInventoryChanged?.Invoke(); // Refresh UI
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

            OnInventoryChanged?.Invoke(); // Refresh UI
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

            OnInventoryChanged?.Invoke(); // Refresh UI
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