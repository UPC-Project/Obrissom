using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; 
using System.Collections.Generic;
using TMPro;

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

    private int _draggedSlotIndex = -1;  // Index of the slot being dragged
    public bool isInventoryOpen = false;
    
    /// Initializes the inventory UI and subscribes to inventory changes.
    /// Also hides the drag icon and closes the inventory at start.
    void Start()
    {
        inventory.OnInventoryChanged += UpdateUI;
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
        {
            SetInventoryState(!isInventoryOpen);
        }

        if (isInventoryOpen)
        {
            MoveItem();
        }
    }

    /// <summary>
    /// Opens or closes the inventory UI.
    /// Also updates cursor visibility and lock state.
    /// </summary>
    public void SetInventoryState(bool isOpen)
    {
        inventoryPanel?.SetActive(isOpen);
        isInventoryOpen = isOpen;
        //Cursor.visible = isOpen;
        //Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Handles drag and drop logic:
    /// - Start dragging
    /// - Move drag icon
    /// - Drop item into another slot
    /// </summary>
    private void MoveItem()
    {
        // 1. Start dragging
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
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

        //Move drag icon with mouse
        if (_draggedSlotIndex != -1 && dragIcon.enabled)
            dragIcon.transform.position = Mouse.current.position.ReadValue();

        // Drop item into another slot
        if (Mouse.current.leftButton.wasReleasedThisFrame && _draggedSlotIndex != -1)
        {
            int destinationIndex = GetSlotUnderMouse();

            if (destinationIndex != -1)
                inventory.MoveItem(_draggedSlotIndex, destinationIndex);

            dragIcon.enabled = false;
            _draggedSlotIndex = -1;
            UpdateUI();
        }

    }

    /// Detects which inventory slot is under the mouse using UI raycast.
    /// Returns the slot index, or -1 if none is found.
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


    //public void DropItemFromSlot() { } TODO

    /// <summary>
    /// Updates the inventory UI:
    /// - Sets item icons
    /// - Updates quantity text
    /// - Clears empty slots
    /// </summary>
    void UpdateUI()
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
}