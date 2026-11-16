using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Player
{
    public class PlayerTeleportService : NetworkBehaviour
    {
        [Tooltip("Distance from the destination for each extra player, so players never land inside each other.")]
        [SerializeField] private float _spawnSpacing = 1.5f;

        private PlayerController _playerController;
        private PlayerSkills _playerSkills;
        private CharacterController _characterController;
        private NetworkTransform _networkTransform;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerSkills = GetComponent<PlayerSkills>();
            _characterController = GetComponent<CharacterController>();
            _networkTransform = GetComponent<NetworkTransform>();
        }

        // Called on the server. The server decides the destination, the owner applies it,
        // because the players NetworkTransform is owner-authoritative.
        public void TeleportTo(Vector3 destination, Quaternion rotation)
        {
            if (!IsServer) return;

            destination += GetSpreadOffset();

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                destination = hit.position;

            TeleportOwnerRpc(destination, rotation);
        }

        private Vector3 GetSpreadOffset()
        {
            IReadOnlyList<ulong> clientIds = NetworkManager.ConnectedClientsIds;
            for (int slot = 1; slot < clientIds.Count; slot++)
            {
                if (clientIds[slot] != OwnerClientId) continue;

                float angle = slot * 90f * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnSpacing;
            }
            return Vector3.zero;
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
            _playerSkills.CancelActiveSkill();
            _characterController.enabled = false;
            _networkTransform.Teleport(destination, rotation, transform.localScale);
            _playerController.SnapCameraTo(rotation);
            yield return null;
            _characterController.enabled = true;
            _playerController.ResetVelocity();
        }
    }
}
