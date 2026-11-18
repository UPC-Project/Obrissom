using UnityEngine;

namespace Obrissom.Enemy
{

    public class MantisBossAnimation : EnemyAnimation
    {
        [Header("Animator parameters")]
        [Tooltip("Bool. True turns the altar into the boss.")]
        [SerializeField] private string _awakeBool = "isAwake";
        [Tooltip("Bool. True plays the death animation.")]
        [SerializeField] private string _deadBool = "isDead";

        [Header("Late join")]
        [Tooltip("Boss idle state (layer 0). A player who joins mid-fight jumps here instead of watching the transformation.")]
        [SerializeField] private string _awakeIdleState = "Idle";

        public void SetAwake(bool awake, bool instant)
        {
            SetBool(_awakeBool, awake);

            if (!awake || !instant || _animator == null || string.IsNullOrEmpty(_awakeIdleState)) return;

            int stateHash = Animator.StringToHash(_awakeIdleState);
            if (_animator.HasState(0, stateHash)) _animator.Play(stateHash, 0, 0f);
        }

        public override void PlayDeathAnimation() => SetBool(_deadBool, true);

        // The boss never walks: skip the Speed parameter of moving enemies
        protected override void Update() { }

        public override void SetStunned(bool stunned) => _isStunned = stunned;

        public override void CancelAttack() { }

        protected void SetBool(string parameter, bool value)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Bool)) _animator.SetBool(parameter, value);
        }

        protected bool HasParameter(string parameter, AnimatorControllerParameterType type)
        {
            if (_animator == null || string.IsNullOrEmpty(parameter)) return false;

            foreach (AnimatorControllerParameter animatorParameter in _animator.parameters)
            {
                if (animatorParameter.type == type && animatorParameter.name == parameter) return true;
            }
            return false;
        }
    }
}
