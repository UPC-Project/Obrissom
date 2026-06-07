using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    public class EnemyAnimation : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private NavMeshAgent _agent;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimDead = Animator.StringToHash("Dead");

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            // TODO: Uncomment when animations are ready.
            // UpdateMovementAnimation();
        }

        public void PlayAttackAnimation(){}

        public void PlayHitAnimation(){}

        public void PlayDeathAnimation(){}

        private void UpdateMovementAnimation()
        {
            if (_animator == null || _agent == null) return;
            _animator.SetFloat(AnimSpeed, _agent.velocity.magnitude);
        }
    }
}