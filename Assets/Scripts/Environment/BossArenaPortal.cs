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

        public ArenaDestinationAnchor DestinationAnchor => _destinationAnchor != null ? _destinationAnchor : ArenaDestinationAnchor.Instance;
        public int RequiredLevel => _requiredLevel;

        public event System.Action<PlayerXP, bool> OnPlayerLevelChecked;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start()
        {
            if (_destinationAnchor == null)
            {
                _destinationAnchor = ArenaDestinationAnchor.Instance;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerXP playerXP = other.GetComponentInParent<PlayerXP>();
            if (playerXP == null || !playerXP.IsOwner) return;

            bool canEnter = HasRequiredLevel(playerXP);
            OnPlayerLevelChecked?.Invoke(playerXP, canEnter);

            if (!canEnter)
            {
                string message = string.Format(_deniedMessage, playerXP.currentLevel, _requiredLevel);
                NotificationPopup.Instance?.ShowMessage(message);
                Debug.Log($"Area unavailable");
            }
            else
            {
                NotificationPopup.Instance?.ShowMessage(_grantedMessage);
                Debug.Log($"Available area");
                RequestTeleport(playerXP);
            }
        }

        private void RequestTeleport(PlayerXP playerXP)
        {
            ArenaDestinationAnchor anchor = DestinationAnchor;
            PlayerTeleportService teleportService = playerXP.GetComponent<PlayerTeleportService>();
            if (anchor == null || teleportService == null) return;

            teleportService.RequestTeleportRpc(anchor.GetSpawnPosition(), anchor.GetSpawnRotation());

            // 3D sound 
            NetworkSoundEmitter soundEmitter = playerXP.GetComponent<NetworkSoundEmitter>();
            if (soundEmitter == null) return;
            soundEmitter.PlayFromOwner(AudioID.PortalTeleport, playerXP.transform.position);
            soundEmitter.PlayFromOwner(AudioID.PortalTeleport, anchor.GetSpawnPosition());
        }

        public bool HasRequiredLevel(PlayerXP playerXP)
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

            ArenaDestinationAnchor targetAnchor = _destinationAnchor != null ? _destinationAnchor : ArenaDestinationAnchor.Instance;
            if (targetAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetAnchor.transform.position);
            }
        }
    }
}
