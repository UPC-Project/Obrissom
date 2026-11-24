using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.UI
{
    /// <summary>
    /// Hides and shows the player HUD with a key. Goes on the HUD canvas.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class HudToggle : MonoBehaviour
    {
        [SerializeField] private Key _toggleKey = Key.H;

        private Canvas _canvas;

        // Used by the game ending to hide the HUD during the cinematic
        public bool Visible
        {
            get => _canvas.enabled;
            set => _canvas.enabled = value;
        }

        private void Awake() => _canvas = GetComponent<Canvas>();

        private void Update()
        {
            // Turning off the canvas hides the HUD but keeps its scripts running
            if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
                _canvas.enabled = !_canvas.enabled;
        }
    }
}
