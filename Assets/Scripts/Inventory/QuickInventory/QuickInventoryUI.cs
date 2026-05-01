using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Obrissom.Player
{
    /// <summary>
    /// Renders the quick inventory weapon slots in the HUD.
    /// Attach to the QuickInventory panel in the Canvas.
    /// </summary>
    public class QuickInventoryUI : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private QuickInventory _quickInventory;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GameObject _quickInventoryPanel;


        private void Start()
        {
            _quickInventory.OnQuickInventoryChanged += UpdateUI;
            UpdateUI();
        }

        private void OnDestroy()
        {
            _quickInventory.OnQuickInventoryChanged -= UpdateUI;
        }

        private void UpdateUI()
        {

            if (_slotContainer == null) return;
            if (_quickInventory == null) return;

            if (!_quickInventoryPanel.activeInHierarchy) return;

            for (int i = 0; i < _slotContainer.childCount; i++)
            {
                Transform slot = _slotContainer.GetChild(i);


                Transform itemTransform = slot.Find("Item");
                Transform qtyTransform = slot.Find("QtyText");

                if (itemTransform == null || qtyTransform == null) continue;
         

                Image itemImage = slot.Find("Item").GetComponent<Image>();
                TextMeshProUGUI qtyText = slot.Find("QtyText").GetComponent<TextMeshProUGUI>();

                if (i < _quickInventory.RingSlots.Count)
                {
                    InventorySlot ringSlot = _quickInventory.RingSlots[i];
                    itemImage.sprite = ringSlot.item.icon;
                    itemImage.enabled = true;
                    itemImage.color = Color.white;
                    qtyText.text = ringSlot.quantity > 1
                        ? ringSlot.quantity.ToString()
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