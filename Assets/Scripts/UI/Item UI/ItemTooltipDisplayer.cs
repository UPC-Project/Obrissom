using UnityEngine;
using UnityEngine.EventSystems;

namespace Obrissom.UI
{
    public class ItemTooltipDisplayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Item _slotItem = null;

        public void SetItem(Item item) => _slotItem = item;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_slotItem == null) return;

            ItemTooltip.Instance.ShowTooltip(_slotItem);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltip.Instance.HideTooltip();
        }

        private void OnDisable()
        {
            if (ItemTooltip.Instance != null)
                ItemTooltip.Instance.HideTooltip();
        }
    }
}

