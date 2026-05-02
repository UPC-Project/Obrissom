using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int _inventorySize = 20;

    [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();
    public IReadOnlyList<InventorySlot> Slots => _slots;

    public event Action OnInventoryChanged;

    /// Creates empty slots when the inventory starts
    private void Awake()
    {
        // TODO: save information
        for (int i = 0; i < _inventorySize; i++)
            _slots.Add(new InventorySlot(null, 0));
    }

    /// <summary>
    /// Adds an item to the inventory.
    /// If the item is stackable, it tries to fill existing stacks first.
    /// If needed, it uses an empty slot.
    /// Returns true if the item was added.
    /// </summary>
    public bool AddItem(Item item, int amount)
    {
        if (item.isStackable)
        {
            foreach (var slot in _slots)
            {
                if (slot.item == item && slot.quantity < item.maxStackSize)
                {
                    int space = item.maxStackSize - slot.quantity;
                    int toAdd = Mathf.Min(amount, space);
                    slot.AddQuantity(toAdd);
                    amount -= toAdd;
                    OnInventoryChanged?.Invoke();

                    if (amount <= 0) return true;
                }
            }
        }

        foreach (var slot in _slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.quantity = amount;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Moves or swaps items between two slots.
    /// </summary>
    public void MoveItem(int sourceIndex, int destinationIndex)
    {
        if (sourceIndex == destinationIndex) return;

        InventorySlot source = _slots[sourceIndex];
        InventorySlot destination = _slots[destinationIndex];

        Item tempItem = destination.item;
        int tempQuantity = destination.quantity;

        destination.item = source.item;
        destination.quantity = source.quantity;

        source.item = tempItem;
        source.quantity = tempQuantity;

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Removes items from a specific slot by index. Supports partial drop on stacks.
    /// amountToDrop = -1 removes the entire stack.
    /// </summary>
    public bool RemoveItemAt(int slotIndex, out Item removedItem, out int removedQuantity, int amountToDrop = -1)
    {
        removedItem = null;
        removedQuantity = 0;

        if (slotIndex < 0 || slotIndex >= _slots.Count) return false;

        InventorySlot slot = _slots[slotIndex];
        if (slot.IsEmpty) return false;

        removedItem = slot.item;

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

}