using UnityEngine;

namespace Obrissom.Environment
{

    public class ArenaDestinationAnchor : MonoBehaviour
    {
        public static ArenaDestinationAnchor Instance { get; private set; }

        [Header("Gizmo Settings")]
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField] private float _forwardRayLength = 3f;

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
