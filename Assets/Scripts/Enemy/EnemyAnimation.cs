using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    public class EnemyAnimation : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        // Bigger jumps are teleports, not movement
        private const float TeleportDistance = 5f;

        [SerializeField] protected Animator _animator;
        [SerializeField] private float _speedSmoothing = 10f;

        protected NavMeshAgent _agent;
        protected bool _isStunned;

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

        public virtual void CancelAttack()
        {
            if (_animator == null) return;

            _animator.ResetTrigger("Attack");

            int attackLayerIndex = _animator.GetLayerIndex("Attack");
            if (attackLayerIndex >= 0)
            {
                int noneStateHash = Animator.StringToHash("none");
                if (_animator.HasState(attackLayerIndex, noneStateHash))
                {
                    _animator.CrossFadeInFixedTime(noneStateHash, 0.1f, attackLayerIndex);
                }
            }
        }

        public virtual void SetStunned(bool stunned)
        {
            _isStunned = stunned;
            if (_isStunned)
            {
                CancelAttack();
                _smoothedSpeed = 0f;
                _lastPosition = transform.position;

                if (_animator != null)
                {
                    _animator.SetFloat(SpeedHash, 0f);
                    _animator.ResetTrigger("Attack");
                    _animator.ResetTrigger("TakeDamage");

                    int idleStateHash = Animator.StringToHash("Idle");
                    if (_animator.HasState(0, idleStateHash))
                    {
                        _animator.CrossFadeInFixedTime(idleStateHash, 0.1f, 0);
                    }
                }
            }
        }

        private void UpdateMovementAnimation()
        {
            if (_animator == null) return;

            if (_isStunned)
            {
                _smoothedSpeed = 0f;
                _animator.SetFloat(SpeedHash, 0f);
                _lastPosition = transform.position;
                return;
            }

            float speed;
            if (_agent != null && _agent.enabled)
            {
                speed = _agent.velocity.magnitude;
            }
            else
            {
                // Clients have no agent, use the synced position
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
