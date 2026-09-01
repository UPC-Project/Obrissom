using UnityEngine;

namespace Obrissom.Enemy
{
    public class SlasherAnimation : EnemyAnimation
    {
        public override void PlayAttackAnimation()        => _animator?.SetTrigger("Attack");
        public override void PlayTakeDamageAnimation()  => _animator?.SetTrigger("TakeDamage");
        public override void PlayDeathAnimation()        => _animator?.SetBool("Dead", true);

    }
}
