using UnityEngine;

namespace Obrissom.Enemy
{
    public class SlasherAnimation : EnemyAnimation
    {
        public override void PlayAttackAnimation()
        {
            if (_isStunned) return;
            _animator?.SetTrigger("Attack");
        }

        public override void PlayTakeDamageAnimation()
        {
            if (_isStunned) return;
            _animator?.SetTrigger("TakeDamage");
        }

        public override void PlayDeathAnimation() => _animator?.SetBool("Dead", true);

    }
}
