using Obrissom.Player;
using UnityEngine;

namespace Obrissom.Environment
{

    [RequireComponent(typeof(Collider))]
    public class BossArenaPortal : MonoBehaviour
    {
        [Header("Destination")]
        [Tooltip("Destination anchor inside the church. If left empty, will automatically locate ArenaDestinationAnchor.Instance.")]
        [SerializeField] private ArenaDestinationAnchor _destinationAnchor;

        [Header("Requirements")]
        [Tooltip("Minimum player level required to enter the church arena.")]
        [SerializeField] private int _requiredLevel = 3;

        [Header("Gizmo Settings")]
        [SerializeField] private Color _gizmoColor = Color.magenta;

        public ArenaDestinationAnchor DestinationAnchor => _destinationAnchor != null ? _destinationAnchor : ArenaDestinationAnchor.Instance;
        public int RequiredLevel => _requiredLevel;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
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
            PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
            if (player != null)
            {
                OnPlayerApproachedPortal(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
            if (player != null)
            {
                OnPlayerLeftPortal(player);
            }
        }

        protected virtual void OnPlayerApproachedPortal(PlayerCombat player)
        {
        }

        protected virtual void OnPlayerLeftPortal(PlayerCombat player)
        {
            // Exit hook
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
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
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 2f);
            }

            // Draw line to destination anchor if available
            ArenaDestinationAnchor targetAnchor = _destinationAnchor != null ? _destinationAnchor : ArenaDestinationAnchor.Instance;
            if (targetAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetAnchor.transform.position);
            }
        }
    }
}
