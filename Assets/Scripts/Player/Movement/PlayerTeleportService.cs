using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerTeleportService : NetworkBehaviour
    {
        private PlayerController _playerController;
        private CharacterController _characterController;
        private NetworkTransform _networkTransform;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _characterController = GetComponent<CharacterController>();
            _networkTransform = GetComponent<NetworkTransform>();
        }

        // Called on the server. Teleports this player to the given position and rotation.
        public void TeleportTo(Vector3 destination, Quaternion rotation)
        {
            if (!IsServer) return;
            StartCoroutine(DoTeleport(destination, rotation));
        }

        private IEnumerator DoTeleport(Vector3 destination, Quaternion rotation)
        {
            _characterController.enabled = false;
            _networkTransform.Teleport(destination, rotation, transform.localScale);
            _playerController.ResetVelocity();
            yield return null;
            _characterController.enabled = true;
        }
    }
}
