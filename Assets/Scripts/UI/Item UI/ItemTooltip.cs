using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.UI
{
    public class ItemTooltip : MonoBehaviour
    {
        public static ItemTooltip Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _itemDescription;

        public Canvas parentCanvas;
        public Transform ToolTipTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (transform.GetChild(0).gameObject.activeSelf)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    Mouse.current.position.ReadValue(),
                    parentCanvas.worldCamera,
                    out Vector2 movePos
                );

                ToolTipTransform.position = parentCanvas.transform.TransformPoint(movePos);
            }
        }

        public void ShowTooltip(Item item)
        {
            _itemName.text = item.itemName;
            _itemDescription.text = item.description;
            transform.GetChild(0).gameObject.SetActive(true);
        }

        public void HideTooltip()
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}

