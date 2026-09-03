using UnityEngine;
using UnityEngine.InputSystem;

/// Handles player interaction input and NPC proximity detection.
namespace Obrissom.Player
{
    public class PlayerInteraction : MonoBehaviour, PlayerInput.IPlayerInteractMapActions
    {
        private NPCInteractable _nearbyNPC;
        private PlayerInput _playerInput;

        private void OnEnable()
        {
            _playerInput = GetComponent<PlayerLocomotionInput>().PlayerInput;
            _playerInput.PlayerInteractMap.Enable();
            _playerInput.PlayerInteractMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            _playerInput.PlayerInteractMap.Disable();
            _playerInput.PlayerInteractMap.RemoveCallbacks(this);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed || _nearbyNPC == null) return;

            _nearbyNPC.OnInteract(GetComponent<PlayerQuestTracker>());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out NPCInteractable npc)) _nearbyNPC = npc;
        }

        private void OnTriggerExit(Collider other)
        {
            _nearbyNPC = null;
        }
    }
}
