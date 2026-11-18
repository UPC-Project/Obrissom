using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Church boss. It starts as an altar and wakes up when a player walks into the church.
    /// It never walks: it only turns to face its target.
    /// </summary>
    [RequireComponent(typeof(MantisBossAnimation))]
    public class MantisBoss : EnemyBase
    {
        [Header("Boss")]
        [Tooltip("Turn towards the target. The boss never walks.")]
        [SerializeField] private bool _faceTarget = true;

        // Synced so late joiners see the boss instead of the altar
        private readonly NetworkVariable<bool> _isAwake = new NetworkVariable<bool>(false);

        private MantisBossAnimation _bossAnimation;

        public bool IsAwake => _isAwake.Value;

        // The altar can't be hurt
        public override bool IsInvulnerable => !_isAwake.Value;

        public override bool CanFaceTarget => _faceTarget && _isAwake.Value;

        // Hits don't interrupt the boss animations
        protected override bool IsStaggerImmune => true;

        protected override void Awake()
        {
            base.Awake();
            _bossAnimation = GetComponent<MantisBossAnimation>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isAwake.OnValueChanged += OnAwakeChanged;

            // Late joiners skip the transformation and go straight to the boss
            _bossAnimation.SetAwake(_isAwake.Value, true);
            _enemyUi.SetHealthBarActive(_isAwake.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isAwake.OnValueChanged -= OnAwakeChanged;
        }

        private void OnAwakeChanged(bool previous, bool current)
        {
            _bossAnimation.SetAwake(current, false);
            _enemyUi.SetHealthBarActive(current);
        }

        /// <summary>
        /// Sent by BossAwakenTrigger from the client of the player who walked in.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestAwakenRpc()
        {
            if (_isAwake.Value || _isDead) return;
            _isAwake.Value = true;
        }

        public override void DetectPlayer()
        {
            // The altar ignores players
            if (!_isAwake.Value)
            {
                _target = null;
                return;
            }

            base.DetectPlayer();
        }

        protected override void ApplyDebuffEffect(DebuffType debuff)
        {
            if (!_isAwake.Value) return;
            base.ApplyDebuffEffect(debuff);
        }

        // Never walks
        public override void OnMoveStateEnter() { }

        public override void OnMoveStateTick() { }

        public override void OnChaseStateTick() { }

        public override float GetMoveSpeed(EnemyState state) => 0f;

        // The melee attack comes with the hand hitboxes
        public override void PerformAttackRpc() { }
    }
}
