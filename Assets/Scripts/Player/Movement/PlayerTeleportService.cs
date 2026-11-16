using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

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

        // Called on the server. The server decides the destination, the owner applies it,
        // because the players NetworkTransform is owner-authoritative.
        public void TeleportTo(Vector3 destination, Quaternion rotation)
        {
            if (!IsServer) return;

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                destination = hit.position;

            TeleportOwnerRpc(destination, rotation);
        }

       
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RequestTeleportRpc(Vector3 destination, Quaternion rotation)
        {
            TeleportTo(destination, rotation);
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void TeleportOwnerRpc(Vector3 destination, Quaternion rotation)
        {
            StartCoroutine(DoTeleport(destination, rotation));
        }

        private IEnumerator DoTeleport(Vector3 destination, Quaternion rotation)
        {
            _characterController.enabled = false;
            _networkTransform.Teleport(destination, rotation, transform.localScale);
            yield return null;
            _characterController.enabled = true;
            _playerController.ResetVelocity();
        }
    }
}
