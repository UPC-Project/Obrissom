using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Obrissom.Audio
{
    /// <summary>
    /// Plays local 2D sounds when a UI Button is hovered or clicked. Add it next to the Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private AudioID _clickSound = AudioID.UIClick;
        [SerializeField] private AudioID _hoverSound = AudioID.UIHover;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button.interactable) Play(_hoverSound);
        }

        private void OnClick() => Play(_clickSound);

        private static void Play(AudioID id)
        {
            if (id != AudioID.None) AudioManager.Instance?.PlaySound(id);
        }
    }
}
