using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// Handles player interaction input and NPC/Item proximity detection.
namespace Obrissom.Player
{
    public class PlayerInteraction : MonoBehaviour, PlayerInput.IPlayerInteractMapActions
    {
        private PlayerQuestTracker _playerQuestTracker;
        private List<NPCInteractable> _nearbyNPCs = new List<NPCInteractable>();
        private List<PickupBase> _nearbyItems = new List<PickupBase>();
        private PlayerInput _playerInput;
        private void Awake()
        {
            _playerQuestTracker = GetComponent<PlayerQuestTracker>();
        }

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
            if (!context.performed) return;

            // Clean up destroyed or disabled objects (items picked up/waiting respawn)
            _nearbyItems.RemoveAll(item => item == null || !item.isActiveAndEnabled || !item.GetComponent<Collider>().enabled);
            _nearbyNPCs.RemoveAll(npc => npc == null || !npc.isActiveAndEnabled);

            if (_nearbyItems.Count > 0)
            {
                _nearbyItems[0].Interact();
                return;
            }

            if (_nearbyNPCs.Count > 0)
            {
                _nearbyNPCs[0].OnInteract(_playerQuestTracker);
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out NPCInteractable npc) && !_nearbyNPCs.Contains(npc))
            {
                _nearbyNPCs.Add(npc);
            }

            if (other.TryGetComponent(out PickupBase item) && !_nearbyItems.Contains(item))
            {
                _nearbyItems.Add(item);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out NPCInteractable npc))
            {
                _nearbyNPCs.Remove(npc);
            }

            if (other.TryGetComponent(out PickupBase item))
            {
                _nearbyItems.Remove(item);
            }
        }
    }
}
