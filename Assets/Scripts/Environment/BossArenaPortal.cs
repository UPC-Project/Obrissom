using Obrissom.Player;
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
            if (playerXP == null) return;

            bool canEnter = HasRequiredLevel(playerXP);
            OnPlayerLevelChecked?.Invoke(playerXP, canEnter);

            if (!canEnter)
            {
                Debug.Log($"[BossArenaPortal] Access denied: Level {playerXP.currentLevel} / {_requiredLevel} required.");
            }
            else
            {
                Debug.Log($"[BossArenaPortal] Access granted: Level {playerXP.currentLevel} meets the requirement.");
            }
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
