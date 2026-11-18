using System.Collections;
using System.Collections.Generic;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    [RequireComponent(typeof(MantisBossAnimation))]
    [RequireComponent(typeof(BossRootPool))]
    public class MantisBoss : EnemyBase
    {
        private const float ArenaCheckInterval = 0.5f;
        private const float GroundProbeHeight = 1f;
        private const float GroundProbeDistance = 6f;

        private static readonly Color WarningColor = new Color(1f, 0.55f, 0.1f, 0.8f);
        private static readonly Color ImpactColor = new Color(1f, 0.1f, 0.05f, 1f);

        [Header("Boss")]
        [Tooltip("Turn towards the target. The boss never walks.")]
        [SerializeField] private bool _faceTarget = true;

        [Tooltip("Off: timers turn the hand hitboxes on and raise the roots (placeholder). " +
                 "On: the attack animations do it through MantisBossAnimationEvents.")]
        [SerializeField] private bool _animationEvents;

        [Tooltip("Seconds of transformation before the first attack.")]
        [SerializeField] private float _wakeUpTime = 2f;

        [Header("Melee")]
        [Tooltip("Hand colliders. Damage comes from the stats (min / max attack damage).")]
        [SerializeField] private BossDamageTrigger[] _meleeHitboxes;
        [Tooltip("From the start of the attack until the hands hit.")]
        [SerializeField] private float _meleeWindup = 0.7f;
        [SerializeField] private float _meleeActiveTime = 0.25f;
        [SerializeField] private float _meleeRecovery = 0.6f;
        [Tooltip("Placeholder warning on the ground in front of the boss (off with Animation Events).")]
        [SerializeField] private float _meleeWarningDistance = 2f;
        [SerializeField] private float _meleeWarningRadius = 1.6f;

        [Header("Roots")]
        [SerializeField] private float _rootDamage = 3f;
        [SerializeField] private float _rootCooldown = 5f;
        [Tooltip("From the start of the cast until the roots come out under the players.")]
        [SerializeField] private float _rootCastTime = 0.5f;
        [SerializeField] private float _rootRecovery = 0.7f;
        [Tooltip("Layers the roots stand on. Player and enemy layers are always skipped.")]
        [SerializeField] private LayerMask _groundMask = Physics.DefaultRaycastLayers;

        [Header("Arena")]
        [Tooltip("Seconds with nobody in the church (standing or down) before the boss heals and turns back into the altar.")]
        [SerializeField] private float _resetDelay = 4f;

        [Header("Victory")]
        [Tooltip("{0} = boss name")]
        [SerializeField] private string _victoryFormat = "{0} defeated";
        [SerializeField] private float _victoryBannerTime = 5f;

        private readonly NetworkVariable<bool> _isAwake = new NetworkVariable<bool>(false);

        private readonly NetworkVariable<float> _healthSynced = new NetworkVariable<float>(1f);

        private readonly HashSet<PlayerCombat> _meleeHits = new HashSet<PlayerCombat>();
        private readonly List<Vector3> _rootPositions = new List<Vector3>();

        private MantisBossAnimation _bossAnimation;
        private BossRootPool _rootPool;
        private BossAwakenTrigger _arena;
        private ShoebillAreaIndicator _meleeIndicator;
        private Coroutine _meleeWarningRoutine;
        private MantisBossHud _hud;

        private Coroutine _attackRoutine;
        private bool _facingLocked;
        private float _attacksAllowedTime;
        private float _rootReadyTime;
        private float _arenaCheckTimer;
        private float _emptyArenaTime;

        public bool IsAwake => _isAwake.Value;

        public override bool IsInvulnerable => !_isAwake.Value;

        public override bool IsImmovable => true;

        public override bool CanFaceTarget => _faceTarget && _isAwake.Value && !_facingLocked;

        protected override bool IsStaggerImmune => true;

        private string BossName => _stats != null && !string.IsNullOrEmpty(_stats.enemyName) ? _stats.enemyName : "Mantis";

        private int GroundMask => _groundMask.value & ~_playerLayer.value & ~(1 << gameObject.layer);

        protected override void Awake()
        {
            base.Awake();
            _bossAnimation = GetComponent<MantisBossAnimation>();
            _rootPool = GetComponent<BossRootPool>();

            foreach (BossDamageTrigger hitbox in _meleeHitboxes)
            {
                if (hitbox != null) hitbox.CanHit = player => _meleeHits.Add(player);
            }
        }

        public void SetArena(BossAwakenTrigger arena) => _arena = arena;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isAwake.OnValueChanged += OnAwakeChanged;
            _healthSynced.OnValueChanged += OnHealthChanged;

            _rootPool.Build();

            if (IsClient)
            {
                _meleeIndicator = ShoebillAreaIndicator.CreateFallback(null);

                _hud = FindAnyObjectByType<MantisBossHud>(FindObjectsInactive.Include);
                if (_hud != null)
                {
                    _hud.SetBossName(BossName);
                    _hud.SetHealth(_healthSynced.Value, true);
                }
                else
                {
                    Debug.LogWarning("[MantisBoss] No MantisBossHud in the scene: the boss health bar won't show.", this);
                }
            }

            _bossAnimation.SetAwake(_isAwake.Value, true);
        }

        public override void OnNetworkDespawn()
        {
            _isAwake.OnValueChanged -= OnAwakeChanged;
            _healthSynced.OnValueChanged -= OnHealthChanged;
            CleanUpLocalObjects();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            CleanUpLocalObjects();
            base.OnDestroy();
        }

        private void CleanUpLocalObjects()
        {
            if (_rootPool != null) _rootPool.Clear();
            if (_meleeIndicator != null) Destroy(_meleeIndicator.gameObject);
            _meleeIndicator = null;

            if (_hud != null) _hud.SetBarVisible(false);
            _hud = null;
        }

        private void OnAwakeChanged(bool previous, bool current)
        {
            _bossAnimation.SetAwake(current, false);
            if (!current) _bossAnimation.ResetAttacks();
        }

        private void OnHealthChanged(float previous, float current)
        {
            if (_hud != null) _hud.SetHealth(current, current > previous);
        }

        protected override void Update()
        {
            base.Update();

            if (_hud != null) _hud.SetBarVisible(_isAwake.Value && _healthSynced.Value > 0f && IsLocalPlayerInArena());
            if (IsServer) CheckForEmptyArena();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestAwakenRpc()
        {
            if (_isAwake.Value || _isDead) return;

            _isAwake.Value = true;
            _emptyArenaTime = 0f;
            _attacksAllowedTime = Time.time + _wakeUpTime;
            _rootReadyTime = _attacksAllowedTime;
        }

        public override void DetectPlayer()
        {
            if (!_isAwake.Value)
            {
                _target = null;
                return;
            }

            if (ForcedTarget != null)
            {
                _target = ForcedTarget;
                return;
            }

            PlayerCombat nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                PlayerCombat player = GetFighter(client);
                if (player == null) continue;

                float sqr = (player.transform.position - transform.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = player;
                }
            }

            _target = nearest != null ? nearest.transform : null;
        }

        public override bool IsPlayerInChaseRange() =>
            _target != null && IsValidTarget(_target) && IsInArena(_target.position);

        private PlayerCombat GetFighter(NetworkClient client)
        {
            if (client.PlayerObject == null) return null;

            PlayerCombat player = client.PlayerObject.GetComponent<PlayerCombat>();
            return player != null && player.IsTargetable && IsInArena(player.transform.position) ? player : null;
        }

        private bool IsInArena(Vector3 position) => _arena != null && _arena.Contains(position);

        private bool IsLocalPlayerInArena()
        {
            NetworkObject localPlayer = NetworkManager != null && NetworkManager.LocalClient != null
                ? NetworkManager.LocalClient.PlayerObject
                : null;
            return localPlayer != null && IsInArena(localPlayer.transform.position);
        }

        protected override void ApplyDebuffEffect(DebuffType debuff)
        {
            if (!_isAwake.Value) return;
            base.ApplyDebuffEffect(debuff);
        }

        public override void OnMoveStateEnter() { }

        public override void OnMoveStateTick() { }

        public override float GetMoveSpeed(EnemyState state) => 0f;

        public override void PerformAttackRpc() => TryStartAttack(true);

        public override void OnChaseStateTick() => TryStartAttack(false);

        private void TryStartAttack(bool targetInMeleeRange)
        {
            if (!IsServer || _isDead || !_isAwake.Value || _attackRoutine != null) return;
            if (Time.time < _attacksAllowedTime) return;

            if (targetInMeleeRange && _attackCooldownTimer <= 0f)
                _attackRoutine = StartCoroutine(MeleeRoutine());
            else if (Time.time >= _rootReadyTime)
                _attackRoutine = StartCoroutine(RootRoutine());
        }

        private IEnumerator MeleeRoutine()
        {
            _attackCooldownTimer = _meleeWindup + _meleeActiveTime + _meleeRecovery + _stats.attackCooldown;
            _meleeHits.Clear();

            float damage = RollAttackDamage();
            foreach (BossDamageTrigger hitbox in _meleeHitboxes)
            {
                if (hitbox != null) hitbox.Damage = damage;
            }

            _facingLocked = true;
            Vector3 warningCenter = transform.position + transform.forward * _meleeWarningDistance;
            PlayMeleeRpc(warningCenter, NetworkManager.ServerTime.Time, !_animationEvents);

            if (_animationEvents)
            {
                yield return new WaitForSeconds(_meleeWindup + _meleeActiveTime);
                _facingLocked = false;
                yield return new WaitForSeconds(_meleeRecovery);
                SetMeleeHitboxes(false);
            }
            else
            {
                yield return new WaitForSeconds(_meleeWindup);
                SetMeleeHitboxes(true);
                yield return new WaitForSeconds(_meleeActiveTime);
                SetMeleeHitboxes(false);
                _facingLocked = false;
                yield return new WaitForSeconds(_meleeRecovery);
            }

            _attackRoutine = null;
        }

        private IEnumerator RootRoutine()
        {
            _rootReadyTime = Time.time + _rootCastTime + _rootRecovery + _rootCooldown;
            PlayRootsAnimationRpc();

            yield return new WaitForSeconds(_rootCastTime);
            if (!_animationEvents) CastRoots();

            yield return new WaitForSeconds(_rootRecovery);
            _attackRoutine = null;
        }

        public void SetMeleeHitboxes(bool active)
        {
            foreach (BossDamageTrigger hitbox in _meleeHitboxes)
            {
                if (hitbox != null) hitbox.SetActive(active);
            }
        }

        public void CastRoots()
        {
            if (!IsServer || _isDead || !_isAwake.Value) return;

            _rootPositions.Clear();
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                PlayerCombat player = GetFighter(client);
                if (player != null) _rootPositions.Add(GetGroundPoint(player.transform.position));
            }

            if (_rootPositions.Count > 0) SpawnRootsRpc(_rootPositions.ToArray(), NetworkManager.ServerTime.Time);
        }

        private Vector3 GetGroundPoint(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeDistance, GroundMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
        }

        public override void CancelAttack()
        {
            base.CancelAttack();
            StopAttack();
        }

        private void StopAttack()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _facingLocked = false;
            SetMeleeHitboxes(false);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayMeleeRpc(Vector3 warningCenter, double startServerTime, bool showWarning)
        {
            _bossAnimation.PlayMeleeAnimation();

            if (!showWarning || _meleeIndicator == null) return;

            StopMeleeWarning();
            _meleeWarningRoutine = StartCoroutine(MeleeWarningRoutine(warningCenter, startServerTime));
        }

        private void StopMeleeWarning()
        {
            if (_meleeWarningRoutine != null)
            {
                StopCoroutine(_meleeWarningRoutine);
                _meleeWarningRoutine = null;
            }
            if (_meleeIndicator != null) _meleeIndicator.Hide();
        }

        private IEnumerator MeleeWarningRoutine(Vector3 center, double startServerTime)
        {
            _meleeIndicator.ShowTelegraph(center, _meleeWarningRadius, startServerTime, _meleeWindup,
                                          WarningColor, ImpactColor, GroundMask);

            double impactTime = startServerTime + _meleeWindup;
            while (_meleeIndicator != null && NetworkManager != null && NetworkManager.ServerTime.Time < impactTime)
                yield return null;

            if (_meleeIndicator != null)
                _meleeIndicator.PlayImpact(center, _meleeWarningRadius, _meleeActiveTime + 0.15f, ImpactColor, GroundMask);
            _meleeWarningRoutine = null;
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayRootsAnimationRpc() => _bossAnimation.PlayRootsAnimation();

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void SpawnRootsRpc(Vector3[] positions, double startServerTime)
        {
            foreach (Vector3 position in positions) _rootPool.Spawn(position, startServerTime, _rootDamage, GroundMask);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void RecallAttacksRpc()
        {
            _rootPool.RecallAll();
            StopMeleeWarning();
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ShowVictoryRpc()
        {
            if (_hud != null) _hud.ShowVictory(string.Format(_victoryFormat, BossName), _victoryBannerTime);
        }

        protected override void OnTakeDamage(float rawAmount)
        {
            _healthSynced.Value = _currentHealth / _stats.maxHealth;
        }

        private void CheckForEmptyArena()
        {
            if (_isDead || !_isAwake.Value)
            {
                _emptyArenaTime = 0f;
                return;
            }

            _arenaCheckTimer -= Time.deltaTime;
            if (_arenaCheckTimer > 0f) return;
            _arenaCheckTimer = ArenaCheckInterval;

            if (AnyPlayerInArena())
            {
                _emptyArenaTime = 0f;
                return;
            }

            _emptyArenaTime += ArenaCheckInterval;
            if (_emptyArenaTime >= _resetDelay) ResetToAltar();
        }

        private bool AnyPlayerInArena()
        {
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject != null && IsInArena(client.PlayerObject.transform.position)) return true;
            }
            return false;
        }

        private void ResetToAltar()
        {
            StopAttack();
            RecallAttacksRpc();

            _emptyArenaTime = 0f;
            _attackCooldownTimer = 0f;
            _target = null;

            _currentHealth = _stats.maxHealth;
            _healthSynced.Value = 1f;

            _isAwake.Value = false;
            _stateMachine.ChangeState(EnemyState.Idle);
        }

        protected override void Die(NetworkObjectReference attackerRef)
        {
            StopAttack();
            base.Die(attackerRef);

            RecallAttacksRpc();
            ShowVictoryRpc();
        }
    }
}
