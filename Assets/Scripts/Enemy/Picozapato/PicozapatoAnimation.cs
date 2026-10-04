using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Picozapato animator bridge. Parameters missing in the controller are skipped instead of logging warnings,
    /// so new clips can be added to the controller progressively.
    /// The area attack falls back to the basic attack trigger until a dedicated clip exists.
    /// </summary>
    public class PicozapatoAnimation : EnemyAnimation
    {
        [Header("Parameters")]
        [SerializeField] private string _basicAttackTrigger = "Attack";
        [SerializeField] private string _areaWindupTrigger = "AreaWindup";
        [SerializeField] private string _areaImpactTrigger = "AreaImpact";
        [Tooltip("Optional bool, true while the hand is in the ground (telegraph).")]
        [SerializeField] private string _rootedBool = "Rooted";
        [SerializeField] private string _takeDamageTrigger = "TakeDamage";
        [SerializeField] private string _deadBool = "Dead";

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

            if (_areaWindupHash == 0) _areaWindupHash = _basicAttackHash;
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

        // Returns 0 if the controller doesn't have the parameter
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
