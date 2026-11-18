using TMPro;
using UnityEngine;

namespace Obrissom.UI
{
    public class NotificationPopup : InformationPopUp
    {
        public static NotificationPopup Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _messageText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
            StartCoroutine(Show());
        }
    }
}
