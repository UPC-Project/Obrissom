using System.Collections.Generic;
using Obrissom.Enemy;
using UnityEngine;

namespace Obrissom.Environment
{
    /// Checkpoint. Resting here (interact key) restores health and resource and makes it that players respawn point.
    /// Each player keeps their own respawn point. Not networked: the server finds it by id (see PlayerRespawn.RestRpc).
    /// The trigger collider is the area where the player can rest.
    [RequireComponent(typeof(Collider))]
    public class Bonfire : MonoBehaviour, IInteractable
    {
        [Tooltip("Unique per bonfire in the scene.")]
        [SerializeField] private string _bonfireId;
        [Tooltip("Where the player appears when respawning here. If empty, 2m in front of the bonfire.")]
        [SerializeField] private Transform _respawnPoint;
        [SerializeField] private GameObject _interactPrompt;

        public Vector3 Position => transform.position;

        public void SetInteractPromptActive(bool active)
        {
            if (_interactPrompt != null)
                _interactPrompt.SetActive(active);
        }

        [Header("Enemies nearby")]
        [Tooltip("Resting is blocked while a living enemy is this close. 0 = never blocked.")]
        [SerializeField] private float _enemyCheckRadius = 12f;
        [SerializeField] private LayerMask _enemyLayer;

        // Extra distance the server accepts outside the trigger (latency of the players position)
        private const float RestRangeMargin = 1.5f;

        private static readonly Dictionary<string, Bonfire> s_bonfires = new Dictionary<string, Bonfire>();

        private Collider _collider;

        public string BonfireId => _bonfireId;
        public Vector3 RespawnPosition => _respawnPoint != null ? _respawnPoint.position : transform.position + transform.forward * 2f;
        public Quaternion RespawnRotation => _respawnPoint != null ? _respawnPoint.rotation : transform.rotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_bonfires.Clear();

        public static Bonfire FindById(string bonfireId)
        {
            if (string.IsNullOrEmpty(bonfireId)) return null;
            return s_bonfires.TryGetValue(bonfireId, out Bonfire bonfire) ? bonfire : null;
        }

        private void Reset()
        {
            _enemyLayer = LayerMask.GetMask("Enemy");
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_bonfireId)) return;
   
            if (s_bonfires.ContainsKey(_bonfireId)) return;


            s_bonfires.Add(_bonfireId, this);
        }

        private void OnDisable()
        {
            if (FindById(_bonfireId) == this) s_bonfires.Remove(_bonfireId);
        }

        /// Server check: is this position inside the rest area (with a small margin)?
        public bool IsInRestRange(Vector3 position)
        {
            Vector3 closest = _collider.ClosestPoint(position);
            return (closest - position).sqrMagnitude <= RestRangeMargin * RestRangeMargin;
        }

        /// Server check: any living enemy close enough to block resting?
        public bool AreEnemiesNearby()
        {
            if (_enemyCheckRadius <= 0f) return false;

            Collider[] hits = Physics.OverlapSphere(transform.position, _enemyCheckRadius, _enemyLayer);
            foreach (Collider hit in hits)
            {
                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy != null && !enemy.IsDead) return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            // Respawn point and facing
            Gizmos.color = new Color(1f, 0.55f, 0f);
            Vector3 respawn = RespawnPosition;
            Vector3 forward = RespawnRotation * Vector3.forward;
            Gizmos.DrawWireSphere(respawn, 0.5f);
            Gizmos.DrawRay(respawn + Vector3.up * 0.1f, forward * 2f);
            Gizmos.DrawLine(transform.position, respawn);
        }

        private void OnDrawGizmosSelected()
        {
            if (_enemyCheckRadius <= 0f) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _enemyCheckRadius);
        }
    }
}
