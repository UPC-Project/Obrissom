using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Shoebill animations. Missing animator parameters are ignored.
    /// </summary>
    public class ShoebillAnimation : EnemyAnimation
    {
        [Header("Parameters")]
        [SerializeField] private string _basicAttackTrigger = "Attack";
        [SerializeField] private string _areaWindupTrigger = "AreaWindup";
        [SerializeField] private string _areaImpactTrigger = "AreaImpact";
        [SerializeField] private string _rootedBool = "Rooted";
        [SerializeField] private string _takeDamageTrigger = "TakeDamage";
        [SerializeField] private string _deadBool = "Dead";

        [Header("Cancel")]
        [Tooltip("Layer with the attack animations. Empty = base layer.")]
        [SerializeField] private string _attackLayerName = "Attack";
        [Tooltip("State to go back to when an attack is cancelled.")]
        [SerializeField] private string _attackLayerIdleState = "none";
        [SerializeField, Min(0f)] private float _cancelBlendDuration = 0.1f;

        private int _attackLayerIndex = -1;
        private int _attackLayerIdleHash;

        private int _basicAttackHash;
        private int _areaWindupHash;
        private int _areaImpactHash;
        private int _rootedHash;
        private int _takeDamageHash;
        private int _deadHash;

        protected override void Awake()
        {
            base.Awake();
            if (_animator == null) return;

            _basicAttackHash = ResolveParameter(_basicAttackTrigger);
            _areaWindupHash = ResolveParameter(_areaWindupTrigger);
            _areaImpactHash = ResolveParameter(_areaImpactTrigger);
            _rootedHash = ResolveParameter(_rootedBool);
            _takeDamageHash = ResolveParameter(_takeDamageTrigger);
            _deadHash = ResolveParameter(_deadBool);

            // No area clip yet: use the basic attack
            if (_areaWindupHash == 0) _areaWindupHash = _basicAttackHash;

            _attackLayerIndex = string.IsNullOrEmpty(_attackLayerName) ? 0 : _animator.GetLayerIndex(_attackLayerName);
            _attackLayerIdleHash = Animator.StringToHash(_attackLayerIdleState);
            if (_attackLayerIndex >= 0 && !_animator.HasState(_attackLayerIndex, _attackLayerIdleHash))
                _attackLayerIndex = -1;
        }

        public void CancelAttack()
        {
            if (_animator == null) return;

            ResetTrigger(_basicAttackHash);
            ResetTrigger(_areaWindupHash);
            ResetTrigger(_areaImpactHash);
            SetRooted(false);

            if (_attackLayerIndex >= 0)
                _animator.CrossFadeInFixedTime(_attackLayerIdleHash, _cancelBlendDuration, _attackLayerIndex);
        }

        private void ResetTrigger(int hash)
        {
            if (hash != 0) _animator.ResetTrigger(hash);
        }

        public override void PlayAttackAnimation() => SetTrigger(_basicAttackHash);

        public void PlayAreaWindup() => SetTrigger(_areaWindupHash);

        public void PlayAreaImpact() => SetTrigger(_areaImpactHash);

        public void SetRooted(bool rooted)
        {
            if (_animator != null && _rootedHash != 0) _animator.SetBool(_rootedHash, rooted);
        }

        public override void PlayTakeDamageAnimation() => SetTrigger(_takeDamageHash);

        public override void PlayDeathAnimation()
        {
            SetRooted(false);
            if (_animator != null && _deadHash != 0) _animator.SetBool(_deadHash, true);
        }

        private void SetTrigger(int hash)
        {
            if (_animator != null && hash != 0) _animator.SetTrigger(hash);
        }

        // 0 if the parameter doesn't exist
        private int ResolveParameter(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return 0;

            int hash = Animator.StringToHash(parameterName);
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
                if (parameter.nameHash == hash) return hash;

            return 0;
        }
    }
}
