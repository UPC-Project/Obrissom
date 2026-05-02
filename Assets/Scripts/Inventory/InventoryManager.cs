using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

namespace Obrissom.Player.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Connections")]
        public Inventory inventory;
        public Transform slotContainer;
        public GameObject inventoryPanel;

        [Header("Test Items (Press G)")]
        public Item testItem1;
        public int amount1 = 1;
        public Item testItem2;
        public int amount2 = 1;
        public Item testItem3;
        public int amount3 = 1;

        [Header("Drag Visuals")]
        public Image dragIcon;

        [Header("Drop")]
        [SerializeField] private ItemDropper _itemDropper;

        [Header("Equipment")]
        [SerializeField] private EquipmentInventory _equipmentInventory;
        [SerializeField] private Transform _equipmentContainer;

        private int _draggedSlotIndex = -1;
        public bool isInventoryOpen = false;

        private bool _draggingFromEquipment = false;
        private int _draggedEquipmentSlotIndex = -1;

        void Start()
        {
            inventory.OnInventoryChanged += UpdateUI;
            _equipmentInventory.OnEquipmentChanged += UpdateEquipmentUI;

            UpdateUI();
            dragIcon.enabled = false;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                inventory.AddItem(testItem1, amount1);
                inventory.AddItem(testItem2, amount2);
                inventory.AddItem(testItem3, amount3);
            }

            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                SetInventoryState(!isInventoryOpen);

            if (isInventoryOpen)
                MoveItem();
        }

        public void SetItemDropper(ItemDropper itemDropper)
        {
            _itemDropper = itemDropper;
        }

        /// <summary>
        /// Opens or closes the inventory UI.
        /// </summary>
        public void SetInventoryState(bool isOpen)
        {
            inventoryPanel?.SetActive(isOpen);
            isInventoryOpen = isOpen;
        }

        /// <summary>
        /// Handles all drag and drop logic for both inventory and equipment slots.
        /// </summary>
        private void MoveItem()
        {
            // 1. Start dragging
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                int equipIndex = GetEquipmentSlotUnderMouse();

                if (equipIndex != -1)
                {
                    InventorySlot equipSlot = _equipmentInventory.GetSlotByIndex(equipIndex);
                    if (equipSlot != null && !equipSlot.IsEmpty)
                    {
                        _draggingFromEquipment = true;
                        _draggedEquipmentSlotIndex = equipIndex;
                        _draggedSlotIndex = 0;
                        dragIcon.sprite = equipSlot.item.icon;
                        dragIcon.enabled = true;
                    }
                }
                else
                {
                    _draggingFromEquipment = false;
                    _draggedSlotIndex = GetSlotUnderMouse();

                    if (_draggedSlotIndex != -1 && !inventory.Slots[_draggedSlotIndex].IsEmpty)
                    {
                        dragIcon.sprite = inventory.Slots[_draggedSlotIndex].item.icon;
                        dragIcon.enabled = true;

                        slotContainer.GetChild(_draggedSlotIndex)
                            .Find("Item").GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
                    }
                    else
                    {
                        _draggedSlotIndex = -1;
                    }
                }
            }

            // 2. Move drag icon with mouse
            if ((_draggedSlotIndex != -1 || _draggingFromEquipment) && dragIcon.enabled)
                dragIcon.transform.position = Mouse.current.position.ReadValue();

            // 3. Drop
            if (Mouse.current.leftButton.wasReleasedThisFrame && (_draggedSlotIndex != -1 || _draggingFromEquipment))
            {
                if (_draggingFromEquipment)
                {
                    int equipIndex = GetEquipmentSlotUnderMouse();
                    if (equipIndex != -1)
                    {
                        _equipmentInventory.MoveEquipment(_draggedEquipmentSlotIndex, equipIndex);
                    }
                    else
                    {
                        int destinationIndex = GetSlotUnderMouse();
                        if (destinationIndex != -1)
                            _equipmentInventory.Unequip(_draggedEquipmentSlotIndex);
                    }
                }
                else
                {
                    // Origen: inventario → destino: equipment o inventario
                    int equipIndex = GetEquipmentSlotUnderMouse();
                    if (equipIndex != -1)
                        _equipmentInventory.Equip(_draggedSlotIndex, equipIndex);
                    else
                    {
                        int destinationIndex = GetSlotUnderMouse();
                        if (destinationIndex != -1)
                            inventory.MoveItem(_draggedSlotIndex, destinationIndex);
                    }
                }

                dragIcon.enabled = false;
                _draggedSlotIndex = -1;
                _draggingFromEquipment = false;
                _draggedEquipmentSlotIndex = -1;
                UpdateUI();
                UpdateEquipmentUI();
            }

            // 4. Right click — drop to world
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                int slotIndex = GetSlotUnderMouse();

                if (slotIndex != -1 && !inventory.Slots[slotIndex].IsEmpty)
                {
                    if (inventory.RemoveItemAt(slotIndex, out Item item, out int qty) && _itemDropper)
                        _itemDropper.DropItem(item, qty);
                }
            }
        }

        /// <summary>
        /// Detects which inventory slot is under the mouse. Returns -1 if none.
        /// </summary>
        private int GetSlotUnderMouse()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                Transform hit = result.gameObject.transform;
                for (int i = 0; i < slotContainer.childCount; i++)
                {
                    Transform slotChild = slotContainer.GetChild(i);
                    if (hit == slotChild || hit.parent == slotChild)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Detects which equipment slot is under the mouse. Returns -1 if none.
        /// </summary>
        private int GetEquipmentSlotUnderMouse()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                Transform hit = result.gameObject.transform;
                for (int i = 0; i < _equipmentContainer.childCount; i++)
                {
                    Transform slotChild = _equipmentContainer.GetChild(i);
                    if (hit == slotChild || hit.parent == slotChild)
                        return i;
                }
            }
            return -1;
        }

        // TODO: Implement DropItemFromSlot for equipment slots

        /// <summary>
        /// Redraws all inventory slots.
        /// </summary>
        private void UpdateUI()
        {
            for (int i = 0; i < slotContainer.childCount; i++)
            {
                Transform slotTransform = slotContainer.GetChild(i);
                Image itemImage = slotTransform.Find("Item").GetComponent<Image>();
                TextMeshProUGUI qtyText = slotTransform.Find("QtyText").GetComponent<TextMeshProUGUI>();

                if (i < inventory.Slots.Count && !inventory.Slots[i].IsEmpty)
                {
                    itemImage.sprite = inventory.Slots[i].item.icon;
                    itemImage.enabled = true;
                    itemImage.color = Color.white;
                    qtyText.text = inventory.Slots[i].quantity > 1
                        ? inventory.Slots[i].quantity.ToString()
                        : "";
                }
                else
                {
                    itemImage.sprite = null;
                    itemImage.enabled = false;
                    qtyText.text = "";
                }
            }
        }

        /// <summary>
        /// Redraws all equipment slots.
        /// </summary>
        private void UpdateEquipmentUI()
        {
            UpdateEquipmentSlot(0, _equipmentInventory.RingSlot1);
            UpdateEquipmentSlot(1, _equipmentInventory.RingSlot2);
            UpdateEquipmentSlot(2, _equipmentInventory.RingSlot3);
        }

        private void UpdateEquipmentSlot(int index, InventorySlot slot)
        {
            if (index >= _equipmentContainer.childCount) return;

            Transform slotTransform = _equipmentContainer.GetChild(index);
            Image itemImage = slotTransform.Find("Item").GetComponent<Image>();

            if (!slot.IsEmpty)
            {
                itemImage.sprite = slot.item.icon;
                itemImage.enabled = true;
                itemImage.color = Color.white;
            }
            else
            {
                itemImage.sprite = null;
                itemImage.enabled = false;
            }
        }
    }
}