using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Environment
{

    public class ArenaDestinationAnchor : MonoBehaviour
    {
        public static ArenaDestinationAnchor Instance { get; private set; }

        [Header("Spawn Settings")]
        [Tooltip("Maximum radius around the anchor to spread arriving players.")]
        [SerializeField] private float _arrivalRadius = 2f;

        [Header("Gizmo Settings")]
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField] private float _forwardRayLength = 3f;

        public float ArrivalRadius => _arrivalRadius;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }


        public Vector3 GetSpawnPosition(int offsetIndex = 0)
        {
            Vector3 targetPos = transform.position;

            if (offsetIndex > 0)
            {
                float angle = offsetIndex * (360f / 4f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (_arrivalRadius * 0.6f);
                targetPos += offset;
            }

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return targetPos;
        }

        public Quaternion GetSpawnRotation() => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _arrivalRadius);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * _forwardRayLength);

            // Draw directional arrow on gizmo
            Vector3 forward = transform.forward * _forwardRayLength;
            Vector3 right = transform.right * 0.5f;
            Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.7f + right);
            Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.7f - right);
        }
    }
}
