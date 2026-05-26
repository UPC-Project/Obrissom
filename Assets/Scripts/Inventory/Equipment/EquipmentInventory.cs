using System;
using UnityEngine;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// Manages equipment slots. Each slot only accepts items of a specific EquipmentSlotType.
    /// Lives alongside Inventory on the player — not a separate system.
    /// </summary>
    public class EquipmentInventory : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private Inventory _inventory;

        public event Action OnEquipmentChanged;

        private InventorySlot _equipmentSlot1;
        private InventorySlot _equipmentSlot2;
        private InventorySlot _equipmentSlot3;

        public InventorySlot EquipmentSlot1 => _equipmentSlot1;
        public InventorySlot EquipmentSlot2 => _equipmentSlot2;
        public InventorySlot EquipmentSlot3 => _equipmentSlot3;

        private void Awake()
        {
            _equipmentSlot1 = new InventorySlot(null, 0, SlotType.Equipment, Item.EquipmentSlotType.Ring);
            _equipmentSlot2 = new InventorySlot(null, 0, SlotType.Equipment, Item.EquipmentSlotType.Ring);
            _equipmentSlot3 = new InventorySlot(null, 0, SlotType.Equipment, Item.EquipmentSlotType.Ring);
        }

        public void SetInventory(Inventory inventory) => _inventory = inventory;

        /// <summary>
        /// Equips an item from the main inventory into the specified equipment slot.
        /// If the slot is already occupied, swaps with the main inventory.
        /// </summary>
        public bool Equip(int inventorySlotIndex, int equipmentSlotIndex)
        {
            InventorySlot source = _inventory.Slots[inventorySlotIndex] as InventorySlot;
            if (source.IsEmpty) return false;

            Item incomingItem = source.item;
            InventorySlot targetSlot = GetSlotByIndex(equipmentSlotIndex);

            if (targetSlot == null) return false;
            if (!targetSlot.CanAccept(incomingItem)) return false;

            if (!targetSlot.IsEmpty)
            {
                bool returned = _inventory.AddItem(targetSlot.item, targetSlot.quantity);
                if (!returned) return false;
            }

            targetSlot.item = incomingItem;
            targetSlot.quantity = source.quantity;

            source.item = null;
            source.quantity = 0;

            ApplyStats(targetSlot.item);

            _inventory.TriggerInventoryChanged();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Unequips an item from the specified slot and returns it to the main inventory.
        /// </summary>
        public bool Unequip(int equipmentSlotIndex)
        {
            InventorySlot slot = GetSlotByIndex(equipmentSlotIndex);
            if (slot == null || slot.IsEmpty) return false;

            bool returned = _inventory.AddItem(slot.item, slot.quantity);
            if (!returned) return false;

            slot.item = null;
            slot.quantity = 0;

            OnEquipmentChanged?.Invoke();
            return true;
        }

        public InventorySlot GetSlotByIndex(int index)
        {
            return index switch
            {
                0 => _equipmentSlot1,
                1 => _equipmentSlot2,
                2 => _equipmentSlot3,
                _ => null
            };
        }

        /// <summary>
        /// Moves an item between two equipment slots.
        /// </summary>
        public bool MoveEquipment(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return false;

            InventorySlot from = GetSlotByIndex(fromIndex);
            InventorySlot to = GetSlotByIndex(toIndex);

            if (from == null || to == null) return false;
            if (from.IsEmpty) return false;

            Item tempItem = to.item;
            int tempQuantity = to.quantity;

            to.item = from.item;
            to.quantity = from.quantity;

            from.item = tempItem;
            from.quantity = tempQuantity;

            OnEquipmentChanged?.Invoke();
            return true;
        }

        public void TriggerEquipmentChanged() => OnEquipmentChanged?.Invoke();

        private void ApplyStats(Item item)
        {
            // TODO
        }

        private void RemoveStats(Item item)
        {
            // TODO: should be called in unequip
        }
    }
}