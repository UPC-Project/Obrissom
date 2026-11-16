using UnityEngine;

namespace Obrissom.Environment
{

    // Arrival point for a portal. Used both inside the church and outside it (exit).
    public class ArenaDestinationAnchor : MonoBehaviour
    {
        [Header("Gizmo Settings")]
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField] private float _forwardRayLength = 3f;

        public Vector3 GetSpawnPosition() => transform.position;

        public Quaternion GetSpawnRotation() => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * _forwardRayLength);

            // Draw directional arrow on gizmo
            Vector3 forward = transform.forward * _forwardRayLength;
            Vector3 right = transform.right * 0.5f;
            Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.7f + right);
            Gizmos.DrawLine(transform.position + forward, transform.position + forward * 0.7f - right);
        }
    }
}
