using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// Renders the equipment slots inside the inventory panel.
    /// Attach to the InventoryPanel or a child of it.
    /// </summary>
    public class EquipmentInventoryUI : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private EquipmentInventory _equipmentInventory;
        [SerializeField] private Transform _equipmentContainer;

        private void Start()
        {
            _equipmentInventory.OnEquipmentChanged += UpdateUI;
            UpdateUI();
        }

        private void OnDestroy()
        {
            _equipmentInventory.OnEquipmentChanged -= UpdateUI;
        }

        private void UpdateUI()
        {
            UpdateSlot(0, _equipmentInventory.RingSlot1);
            UpdateSlot(1, _equipmentInventory.RingSlot2);
            UpdateSlot(2, _equipmentInventory.RingSlot3);
        }

        private void UpdateSlot(int index, InventorySlot slot)
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