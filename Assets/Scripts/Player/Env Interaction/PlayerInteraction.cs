using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Obrissom.Environment;
using Unity.Netcode;

/// Handles player interaction input and NPC/Item/Bonfire proximity detection.
namespace Obrissom.Player
{
    public class PlayerInteraction : MonoBehaviour, PlayerInput.IPlayerInteractMapActions
    {
        private PlayerQuestTracker _playerQuestTracker;
        private PlayerRespawn _playerRespawn;
        private List<NPCInteractable> _nearbyNPCs = new List<NPCInteractable>();
        private List<PickupBase> _nearbyItems = new List<PickupBase>();
        private List<Bonfire> _nearbyBonfires = new List<Bonfire>();
        private PlayerInput _playerInput;
        private NetworkObject _networkObject;

        // Every player copy has this script and reads the local keyboard: only your own player may interact
        private bool IsOwner => _networkObject != null && _networkObject.IsOwner;

        private void Awake()
        {
            _playerQuestTracker = GetComponent<PlayerQuestTracker>();
            _playerRespawn = GetComponent<PlayerRespawn>();
            _networkObject = GetComponent<NetworkObject>();
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
            if (!IsOwner) return;

            // Reviving is held: releasing the key stops it
            if (context.canceled)
            {
                if (_playerRespawn != null) _playerRespawn.CancelRevive();
                return;
            }

            if (!context.performed) return;

            // A downed teammate nearby comes first
            if (_playerRespawn != null && _playerRespawn.TryStartRevive()) return;

            // Clean up destroyed or disabled objects (items picked up/waiting respawn)
            _nearbyItems.RemoveAll(item => item == null || !item.isActiveAndEnabled || !item.GetComponent<Collider>().enabled);
            _nearbyNPCs.RemoveAll(npc => npc == null || !npc.isActiveAndEnabled);
            _nearbyBonfires.RemoveAll(bonfire => bonfire == null || !bonfire.isActiveAndEnabled);

            if (_nearbyItems.Count > 0)
            {
                GetClosestItem().Interact();
                return;
            }

            if (_nearbyNPCs.Count > 0)
            {
                _nearbyNPCs[0].OnInteract(_playerQuestTracker);
                return;
            }

            if (_nearbyBonfires.Count > 0 && _playerRespawn != null)
            {
                _playerRespawn.RequestRest(_nearbyBonfires[0]);
                return;
            }
        }

        // With several items in range, F takes the one you're standing next to (not the first that entered the trigger)
        private PickupBase GetClosestItem()
        {
            PickupBase closest = _nearbyItems[0];
            float closestSqrDistance = float.MaxValue;

            foreach (PickupBase item in _nearbyItems)
            {
                float sqrDistance = (item.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = item;
                }
            }
            return closest;
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

            if (other.TryGetComponent(out Bonfire bonfire) && !_nearbyBonfires.Contains(bonfire))
            {
                _nearbyBonfires.Add(bonfire);
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

            if (other.TryGetComponent(out Bonfire bonfire))
            {
                _nearbyBonfires.Remove(bonfire);
            }
        }
    }
}
