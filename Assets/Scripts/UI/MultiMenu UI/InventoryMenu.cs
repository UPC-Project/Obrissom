using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Obrissom.Player.Inventory;
using TMPro;

/// <summary>
/// Manages the Inventory UI panel: visual feedback, drag & drop, and equipment display.
/// Connects the Player's data (Inventory / EquipmentInventory) with the screen.
/// </summary>
public class InventoryMenu : MenuPanel
{
    [Header("Inventory")]
    [SerializeField] private Transform _slotContainer;

    [Header("Drag & Drop Visuals")]
    [SerializeField] private Image _dragIcon;

    [Header("Equipment")]
    [SerializeField] private EquipmentInventory _equipmentInventory;
    [SerializeField] private Transform _equipmentContainer;

    // Data references (set via BindLocalPlayer)
    private Inventory _inventory;
    private ItemDropper _itemDropper;

    // Internal drag state
    private int _draggedSlotIndex = -1;
    private bool _draggingFromEquipment = false;
    private int _draggedEquipmentSlotIndex = -1;

    void Start()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged += UpdateInventoryUI;

        if (_equipmentInventory != null)
            _equipmentInventory.OnEquipmentChanged += UpdateEquipmentUI;

        UpdateInventoryUI();
        _dragIcon.enabled = false;
    }

    /// <summary>
    /// Links the Local Player's components to this UI.
    /// Called by PlayerUIManager when the player spawns.
    /// </summary>
    public void BindLocalPlayer(Inventory localInventory, ItemDropper localDropper)
    {
        // Unsubscribe from previous player if exists
        if (_inventory != null)
            _inventory.OnInventoryChanged -= UpdateInventoryUI;

        _inventory = localInventory;
        _itemDropper = localDropper;

        _equipmentInventory.SetInventory(_inventory);

        if (_inventory != null)
        {
            _inventory.OnInventoryChanged += UpdateInventoryUI;
            UpdateInventoryUI();
        }
    }

    void Update()
    {
        if (IsOpen)
            MoveItem();
    }

    /// <summary>
    /// Logic for Dragging, Dropping and Swapping items with the mouse.
    /// </summary>
    private void MoveItem()
    {
        // START DRAGGING 
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
                    _dragIcon.sprite = equipSlot.item.image;
                    _dragIcon.color = new Color(1, 1, 1, 0.5f);
                    _dragIcon.enabled = true;
                }
            }
            else if (_inventory != null) // Check if clicking an inventory slot
            {
                _draggingFromEquipment = false;
                _draggedSlotIndex = GetSlotUnderMouse();

                if (_draggedSlotIndex != -1 && !_inventory.Slots[_draggedSlotIndex].IsEmpty)
                {
                    _dragIcon.sprite = _inventory.Slots[_draggedSlotIndex].item.image;
                    _dragIcon.color = new Color(1, 1, 1, 0.5f);
                    _dragIcon.enabled = true;

                    // make the original slot transparent while dragging
                    _slotContainer.GetChild(_draggedSlotIndex)
                        .Find("Item").GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
                }
                else
                {
                    _draggedSlotIndex = -1;
                }
            }
        }

        // UPDATE DRAG ICON POSITION 
        if ((_draggedSlotIndex != -1 || _draggingFromEquipment) && _dragIcon.enabled)
            _dragIcon.transform.position = Mouse.current.position.ReadValue();

        // DROP ITEM
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
            else if (_inventory != null) // Handle drop from inventory
            {
                int equipIndex = GetEquipmentSlotUnderMouse();
                if (equipIndex != -1)
                    _equipmentInventory.Equip(_draggedSlotIndex, equipIndex);
                else
                {
                    int destinationIndex = GetSlotUnderMouse();
                    if (destinationIndex != -1)
                        _inventory.MoveItem(_draggedSlotIndex, destinationIndex);
                }
            }

            // Reset Drag State
            _dragIcon.enabled = false;
            _draggedSlotIndex = -1;
            _draggingFromEquipment = false;
            _draggedEquipmentSlotIndex = -1;
            UpdateInventoryUI();
            UpdateEquipmentUI();
        }

        // RIGHT CLICK (Drop to World)
        if (Mouse.current.rightButton.wasPressedThisFrame && _inventory != null)
        {
            int slotIndex = GetSlotUnderMouse();

            if (slotIndex != -1 && !_inventory.Slots[slotIndex].IsEmpty)
            {
                if (_itemDropper != null)
                {
                    if (_inventory.RemoveItemAt(slotIndex, out Item item, out int qty))
                    {
                        _itemDropper.DropItem(item, qty);
                    }
                }
            }
        }
    }

    // HELPER METHODS: Raycast to find UI Slots

    private int GetSlotUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        { position = Mouse.current.position.ReadValue() };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Transform hit = result.gameObject.transform;
            for (int i = 0; i < _slotContainer.childCount; i++)
            {
                Transform slotChild = _slotContainer.GetChild(i);
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

    // UI REFRESH METHODS 

    private void UpdateInventoryUI()
    {
        if (_inventory == null) return;
        for (int i = 0; i < _slotContainer.childCount; i++)
        {
            Transform slotTransform = _slotContainer.GetChild(i);
            Image itemImage = slotTransform.Find("Item").GetComponent<Image>();
            TextMeshProUGUI qtyText = slotTransform.Find("QtyText").GetComponent<TextMeshProUGUI>();

            if (i < _inventory.Slots.Count && !_inventory.Slots[i].IsEmpty)
            {
                itemImage.sprite = _inventory.Slots[i].item.image;
                itemImage.enabled = true;
                itemImage.color = Color.white;
                qtyText.text = _inventory.Slots[i].quantity > 1 ? _inventory.Slots[i].quantity.ToString() : "";
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
        UpdateEquipmentSlot(0, _equipmentInventory.EquipmentSlot1);
        UpdateEquipmentSlot(1, _equipmentInventory.EquipmentSlot2);
        UpdateEquipmentSlot(2, _equipmentInventory.EquipmentSlot3);
    }

    private void UpdateEquipmentSlot(int index, InventorySlot slot)
    {
        if (index >= _equipmentContainer.childCount) return;

        Transform slotTransform = _equipmentContainer.GetChild(index);
        Image itemImage = slotTransform.Find("Item").GetComponent<Image>();

        if (!slot.IsEmpty)
        {
            itemImage.sprite = slot.item.image;
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

