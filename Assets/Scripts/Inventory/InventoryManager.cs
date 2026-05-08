using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// This class manages the Inventory UI, visual feedback, and mouse interactions.
    /// It connects the Player's data (Inventory) with the screen (Canvas).
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Header("Main Connections")]
        public Inventory inventory;           // Reference to the active player's inventory data
        public Transform slotContainer;       // The UI parent of the inventory slots
        public GameObject inventoryPanel;     // The main UI panel to open/close

        [Header("Test Items (Press G)")]
        public Item testItem1;
        public int amount1 = 1;
        public Item testItem2;
        public int amount2 = 1;
        public Item testItem3;
        public int amount3 = 1;

        [Header("Drag & Drop Visuals")]
        public Image dragIcon;                // The icon that follows the mouse when dragging

        [Header("World Interaction")]
        [SerializeField] private ItemDropper _itemDropper; // Reference to the player's dropper component

        [Header("Equipment UI")]
        [SerializeField] private EquipmentInventory _equipmentInventory;
        [SerializeField] private Transform _equipmentContainer;

        // Internal State
        private int _draggedSlotIndex = -1;
        public bool isInventoryOpen = false;
        private bool _draggingFromEquipment = false;
        private int _draggedEquipmentSlotIndex = -1;

        void Start()
        {
            // Subscribe to data changes to refresh the UI automatically
            if (inventory != null)
                inventory.OnInventoryChanged += UpdateUI;

            if (_equipmentInventory != null)
                _equipmentInventory.OnEquipmentChanged += UpdateEquipmentUI;

            UpdateUI();
            dragIcon.enabled = false; // Hide drag icon at start
        }

        /// <summary>
        /// Links the Local Player's components to this Global UI.
        /// Called when the player spawns.
        /// </summary>
        public void BindLocalPlayer(Inventory localInventory, ItemDropper localDropper)
        {
            // Unsubscribe from previous player if exists
            if (inventory != null)
                inventory.OnInventoryChanged -= UpdateUI;

            inventory = localInventory;
            _itemDropper = localDropper;

            if (inventory != null)
            {
                inventory.OnInventoryChanged += UpdateUI;
                UpdateUI();
            }
        }

        void Update()
        {
            // Debug: Add items for testing
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame && inventory != null)
            {
                inventory.AddItem(testItem1, amount1);
                inventory.AddItem(testItem2, amount2);
                inventory.AddItem(testItem3, amount3);
            }

            // Input: Toggle Inventory UI
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                SetInventoryState(!isInventoryOpen);

            // If UI is open, handle mouse logic
            if (isInventoryOpen)
                MoveItem();
        }

        public void SetInventoryState(bool isOpen)
        {
            inventoryPanel?.SetActive(isOpen);
            isInventoryOpen = isOpen;
        }

        /// <summary>
        /// Logic for Dragging, Dropping and Swapping items with the mouse.
        /// </summary>
        private void MoveItem()
        {
            // --- 1. START DRAGGING ---
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                int equipIndex = GetEquipmentSlotUnderMouse();

                if (equipIndex != -1) // Check if clicking an equipment slot
                {
                    InventorySlot equipSlot = _equipmentInventory.GetSlotByIndex(equipIndex);
                    if (equipSlot != null && !equipSlot.IsEmpty)
                    {
                        _draggingFromEquipment = true;
                        _draggedEquipmentSlotIndex = equipIndex;
                        _draggedSlotIndex = 0; // Temp index to allow dragging
                        dragIcon.sprite = equipSlot.item.icon;
                        dragIcon.enabled = true;
                    }
                }
                else if (inventory != null) // Check if clicking an inventory slot
                {
                    _draggingFromEquipment = false;
                    _draggedSlotIndex = GetSlotUnderMouse();

                    if (_draggedSlotIndex != -1 && !inventory.Slots[_draggedSlotIndex].IsEmpty)
                    {
                        dragIcon.sprite = inventory.Slots[_draggedSlotIndex].item.icon;
                        dragIcon.enabled = true;

                        // Visual feedback: make the original slot transparent while dragging
                        slotContainer.GetChild(_draggedSlotIndex)
                            .Find("Item").GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
                    }
                    else
                    {
                        _draggedSlotIndex = -1;
                    }
                }
            }

            // --- 2. UPDATE DRAG ICON POSITION ---
            if ((_draggedSlotIndex != -1 || _draggingFromEquipment) && dragIcon.enabled)
                dragIcon.transform.position = Mouse.current.position.ReadValue();

            // --- 3. DROP ITEM ---
            if (Mouse.current.leftButton.wasReleasedThisFrame && (_draggedSlotIndex != -1 || _draggingFromEquipment))
            {
                if (_draggingFromEquipment) // Handle drop from equipment
                {
                    int equipIndex = GetEquipmentSlotUnderMouse();
                    if (equipIndex != -1)
                        _equipmentInventory.MoveEquipment(_draggedEquipmentSlotIndex, equipIndex);
                    else
                    {
                        int destinationIndex = GetSlotUnderMouse();
                        if (destinationIndex != -1)
                            _equipmentInventory.Unequip(_draggedEquipmentSlotIndex);
                    }
                }
                else if (inventory != null) // Handle drop from inventory
                {
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

                // Reset Drag State
                dragIcon.enabled = false;
                _draggedSlotIndex = -1;
                _draggingFromEquipment = false;
                _draggedEquipmentSlotIndex = -1;
                UpdateUI();
                UpdateEquipmentUI();
            }

            // --- 4. RIGHT CLICK (Drop to World) ---
            if (Mouse.current.rightButton.wasPressedThisFrame && inventory != null)
            {
                int slotIndex = GetSlotUnderMouse();

                if (slotIndex != -1 && !inventory.Slots[slotIndex].IsEmpty)
                {
                    if (_itemDropper != null)
                    {
                        if (inventory.RemoveItemAt(slotIndex, out Item item, out int qty))
                        {
                            _itemDropper.DropItem(item, qty);
                        }
                    }
                }
            }
        }

        // --- HELPER METHODS: Raycast to find UI Slots ---

        private int GetSlotUnderMouse()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            { position = Mouse.current.position.ReadValue() };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                Transform hit = result.gameObject.transform;
                for (int i = 0; i < slotContainer.childCount; i++)
                {
                    Transform slotChild = slotContainer.GetChild(i);
                    if (hit == slotChild || hit.parent == slotChild) return i;
                }
            }
            return -1;
        }

        private int GetEquipmentSlotUnderMouse()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            { position = Mouse.current.position.ReadValue() };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                Transform hit = result.gameObject.transform;
                for (int i = 0; i < _equipmentContainer.childCount; i++)
                {
                    Transform slotChild = _equipmentContainer.GetChild(i);
                    if (hit == slotChild || hit.parent == slotChild) return i;
                }
            }
            return -1;
        }

        // --- UI REFRESH METHODS ---

        private void UpdateUI()
        {
            if (inventory == null) return;
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
                    qtyText.text = inventory.Slots[i].quantity > 1 ? inventory.Slots[i].quantity.ToString() : "";
                }
                else
                {
                    itemImage.sprite = null;
                    itemImage.enabled = false;
                    qtyText.text = "";
                }
            }
        }

        private void UpdateEquipmentUI()
        {
            if (_equipmentInventory == null) return;
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