using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    public class EnemyAnimation : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        // Larger jumps in a single frame are treated as teleports (spawn, warp), not movement
        private const float TeleportDistance = 5f;

        [SerializeField] protected Animator _animator;
        [SerializeField] private float _speedSmoothing = 10f;

        protected NavMeshAgent _agent;

        private Vector3 _lastPosition;
        private float _smoothedSpeed;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void OnEnable()
        {
            _lastPosition = transform.position;
        }

        protected virtual void Update()
        {
            UpdateMovementAnimation();
        }

        public virtual void PlayAttackAnimation() { }

        public virtual void PlayTakeDamageAnimation() { }

        public virtual void PlayDeathAnimation() { }

        private void UpdateMovementAnimation()
        {
            if (_animator == null) return;

            float speed;
            if (_agent != null && _agent.enabled)
            {
                speed = _agent.velocity.magnitude;
            }
            else
            {
                // Clients: the NavMeshAgent only runs on the server, derive speed from the synced transform
                Vector3 delta = transform.position - _lastPosition;
                delta.y = 0f;
                float distance = delta.magnitude;
                speed = distance < TeleportDistance && Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;
            }
            _lastPosition = transform.position;

            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, speed, 1f - Mathf.Exp(-_speedSmoothing * Time.deltaTime));
            _animator.SetFloat(SpeedHash, _smoothedSpeed);
        }
    }
}
