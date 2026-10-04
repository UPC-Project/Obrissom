using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    public class EnemyAnimation : MonoBehaviour
    {
        [SerializeField] protected Animator _animator;

        protected NavMeshAgent _agent;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
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
            if (_animator == null || _agent == null) return;
            _animator.SetFloat("Speed", _agent.velocity.magnitude);
        }
    }
}