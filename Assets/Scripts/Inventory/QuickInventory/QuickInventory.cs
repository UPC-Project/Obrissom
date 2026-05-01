using System;
using System.Collections.Generic;
using UnityEngine;
using static Item;

namespace Obrissom.Player
{
    /// <summary>
    /// Filters weapons from the main inventory and exposes them as a quick access list.
    /// Max 3 slots. Read-only for now — equip logic pending combat system.
    /// </summary>
    public class QuickInventory : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _maxSlots = 3;

        [Header("Connections")]
        [SerializeField] private Inventory _inventory;

        public event Action OnQuickInventoryChanged;

        // Lista filtrada de armas — máximo _maxSlots
        private List<InventorySlot> _ringSlots = new List<InventorySlot>();
        public IReadOnlyList<InventorySlot> RingSlots => _ringSlots;

        private void Start()
        {
            _inventory.OnInventoryChanged += RefreshRings;
            RefreshRings();
        }

        private void OnDestroy()
        {
            _inventory.OnInventoryChanged -= RefreshRings;
        }

        /// <summary>
        /// Scans the main inventory and updates the weapon slot list.
        /// </summary>
        private void RefreshRings()
        {
            _ringSlots.Clear();

            foreach (var slot in _inventory.Slots)
            {
                if (_ringSlots.Count >= _maxSlots) break;

                if (!slot.IsEmpty && slot.item.itemType == ItemType.Rings)
                    _ringSlots.Add(slot);
            }

            OnQuickInventoryChanged?.Invoke();
        }

    }
}