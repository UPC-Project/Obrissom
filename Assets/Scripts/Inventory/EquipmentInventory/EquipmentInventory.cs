using System;
using System.Collections.Generic;
using UnityEngine;
using static Item;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// Filters weapons from the main inventory and exposes them as a quick access list.
    /// Max 3 slots. Read-only for now — equip logic pending combat system.
    /// </summary>
    public class EquipmentInventory : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _maxSlots = 3;

        [Header("Connections")]
        [SerializeField] private Inventory _inventory;

        public event Action OnEquipmentInventoryChanged;

        // Lista filtrada de armas — máximo _maxSlots
        private List<InventorySlot> _equipmentSlots = new List<InventorySlot>();
        public IReadOnlyList<InventorySlot> EquipmentSlots => _equipmentSlots;

        private void Start()
        {
            _inventory.OnInventoryChanged += RefreshEquippable;
            RefreshEquippable();
        }

        private void OnDestroy()
        {
            _inventory.OnInventoryChanged -= RefreshEquippable;
        }

        /// <summary>
        /// Scans the main inventory and updates the weapon slot list.
        /// </summary>
        private void RefreshEquippable()
        {
            _equipmentSlots.Clear();

            foreach (var slot in _inventory.Slots)
            {
                if (_equipmentSlots.Count >= _maxSlots) break;

                if (!slot.IsEmpty && slot.item.itemType == ItemType.Equippable)
                    _equipmentSlots.Add(slot);
            }

            OnEquipmentInventoryChanged?.Invoke();
        }

    }
}