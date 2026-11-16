using Obrissom.Audio;
using Obrissom.Player;
using Obrissom.UI;
using UnityEngine;

namespace Obrissom.Environment
{
    [RequireComponent(typeof(Collider))]
    public class BossArenaPortal : MonoBehaviour
    {
        [Header("Destination")]
        [SerializeField] private ArenaDestinationAnchor _destinationAnchor;

        [Header("Requirements")]
        [SerializeField] private int _requiredLevel = 3;
        [SerializeField] private string _deniedMessage = "Area unavailable";
        [SerializeField] private string _grantedMessage = "Available area";

        [Header("Gizmo Settings")]
        [SerializeField] private Color _gizmoColor = Color.magenta;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerXP playerXP = other.GetComponentInParent<PlayerXP>();
            if (playerXP == null || !playerXP.IsOwner) return;

            if (!HasRequiredLevel(playerXP))
            {
                string message = string.Format(_deniedMessage, playerXP.currentLevel, _requiredLevel);
                NotificationPopup.Instance?.ShowMessage(message);
                return;
            }

            // Leave empty for portals that need no message (e.g. the church exit)
            if (!string.IsNullOrEmpty(_grantedMessage))
                NotificationPopup.Instance?.ShowMessage(_grantedMessage);

            RequestTeleport(playerXP);
        }

        private void RequestTeleport(PlayerXP playerXP)
        {
            PlayerTeleportService teleportService = playerXP.GetComponent<PlayerTeleportService>();
            if (_destinationAnchor == null || teleportService == null) return;

            Vector3 arrival = _destinationAnchor.GetSpawnPosition();
            teleportService.RequestTeleportRpc(arrival, _destinationAnchor.GetSpawnRotation());

            // 3D sound 
            NetworkSoundEmitter soundEmitter = playerXP.GetComponent<NetworkSoundEmitter>();
            if (soundEmitter == null) return;
            soundEmitter.PlayFromOwner(AudioID.PortalTeleport, playerXP.transform.position);
            soundEmitter.PlayFromOwner(AudioID.PortalTeleport, arrival);
        }

        private bool HasRequiredLevel(PlayerXP playerXP)
        {
            return playerXP.currentLevel >= _requiredLevel;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;

            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (col is SphereCollider sphere)
            {
                float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * maxScale);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 2f);
            }

            if (_destinationAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _destinationAnchor.transform.position);
            }
        }
    }
}
