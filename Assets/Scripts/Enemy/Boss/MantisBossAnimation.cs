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
        [Tooltip("Trigger. Melee attack with the hands.")]
        [SerializeField] private string _meleeTrigger = "Attack";
        [Tooltip("Trigger. Root attack.")]
        [SerializeField] private string _rootsTrigger = "Roots";

        [Header("Late join")]
        [Tooltip("Boss idle state (layer 0). A player who joins mid-fight jumps here instead of watching the transformation.")]
        [SerializeField] private string _awakeIdleState = "Idle";

        protected override void Awake()
        {
            base.Awake();

            if (_animator != null) _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void SetAwake(bool awake, bool instant)
        {
            SetBool(_awakeBool, awake);

            if (!awake || !instant || _animator == null || string.IsNullOrEmpty(_awakeIdleState)) return;

            int stateHash = Animator.StringToHash(_awakeIdleState);
            if (_animator.HasState(0, stateHash)) _animator.Play(stateHash, 0, 0f);
        }

        public override void PlayDeathAnimation() => SetBool(_deadBool, true);

        public void PlayMeleeAnimation() => SetTrigger(_meleeTrigger);

        public void PlayRootsAnimation(string trigger) => SetTrigger(string.IsNullOrEmpty(trigger) ? _rootsTrigger : trigger);

        public void ResetAttacks()
        {
            if (HasParameter(_meleeTrigger, AnimatorControllerParameterType.Trigger)) _animator.ResetTrigger(_meleeTrigger);
            if (HasParameter(_rootsTrigger, AnimatorControllerParameterType.Trigger)) _animator.ResetTrigger(_rootsTrigger);
        }

        protected override void Update() { }

        public override void SetStunned(bool stunned) => _isStunned = stunned;

        public override void CancelAttack() { }

        protected void SetBool(string parameter, bool value)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Bool)) _animator.SetBool(parameter, value);
        }

        protected void SetTrigger(string parameter)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Trigger)) _animator.SetTrigger(parameter);
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
