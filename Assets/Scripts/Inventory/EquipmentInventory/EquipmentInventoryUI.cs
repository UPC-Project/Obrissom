using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Obrissom.UI
{
    /// <summary>
    /// Renders the equipment inventory weapon slots in the HUD.
    /// Attach to the EquipmentInventory panel in the Canvas.
    /// </summary>
    public class EquipmentInventoryUI : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private Player.Inventory.EquipmentInventory _equipmentInventory;
        [SerializeField] private Transform _slotContainer;


        private void Start()
        {
            _equipmentInventory.OnEquipmentInventoryChanged += UpdateUI;
            UpdateUI();
        }

        private void OnDestroy()
        {
            _equipmentInventory.OnEquipmentInventoryChanged -= UpdateUI;
        }

        private void UpdateUI()
        {
            if (_slotContainer == null) return;
            if (_equipmentInventory == null) return;


            for (int i = 0; i < _slotContainer.childCount; i++)
            {
                Transform slot = _slotContainer.GetChild(i);


                Transform itemTransform = slot.Find("Item");
                Transform qtyTransform = slot.Find("QtyText");

                if (itemTransform == null || qtyTransform == null) continue;
         

                Image itemImage = slot.Find("Item").GetComponent<Image>();
                TextMeshProUGUI qtyText = slot.Find("QtyText").GetComponent<TextMeshProUGUI>();

                if (i < _equipmentInventory.EquipmentSlots.Count)
                {
                    Player.Inventory.InventorySlot equipmentSlot = _equipmentInventory.EquipmentSlots[i];
                    itemImage.sprite = equipmentSlot.item.icon;
                    itemImage.enabled = true;
                    itemImage.color = Color.white;
                    qtyText.text = equipmentSlot.quantity > 1
                        ? equipmentSlot.quantity.ToString()
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
}