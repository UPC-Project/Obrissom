using System.Collections;
using System.Collections.Generic;
using Obrissom.Audio;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Slow tank with two attacks:
    /// Dash: marks a strip in front of it, then dashes along it and hits everyone inside.
    /// Ground slam: circles grow around it one after another, each one hits harder and the last one stuns.
    /// The server runs the AI. Clients only draw the ground warnings.
    /// </summary>
    public class SpectreTankEnemy : EnemyBase
    {
        private const int MaxOverlapHits = 16;
        private const float HitBoxHeight = 3f;
        private const float WallPadding = 0.3f;

        private enum Attack { None, Dash, Slam }

        [Header("Spectre Tank")]
        [SerializeField] private SpectreTankConfig _config;

        private static readonly Collider[] s_overlapHits = new Collider[MaxOverlapHits];

        private readonly HashSet<ulong> _hitPlayers = new HashSet<ulong>();
        private readonly List<SpectreTankTelegraph> _telegraphs = new List<SpectreTankTelegraph>();
        private Coroutine _attackRoutine;
        private Attack _currentAttack;
        private float _slamReadyTime;

        private bool IsAttacking => _currentAttack != Attack.None;

        // Hits don't stop an attack, and it doesn't turn once the warning is shown
        protected override bool IsStaggerImmune => IsAttacking;
        public override bool CanFaceTarget => !IsAttacking;

        // Lifecycle

        protected override void Awake()
        {
            base.Awake();
            if (_config == null) Debug.LogError($"[SpectreTank] {name} has no SpectreTankConfig assigned.", this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) ConfigureAgent();
        }

        public override void OnNetworkDespawn()
        {
            DestroyTelegraphs();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            DestroyTelegraphs();
            base.OnDestroy();
        }

        private void ConfigureAgent()
        {
            _agent.acceleration = _config.acceleration;
            _agent.angularSpeed = _config.angularSpeed;
            _agent.stoppingDistance = _config.stoppingDistance;
        }

        // Attack decision

        public override bool IsPlayerInAttackRange() => IsAttacking || CanSlam() || CanDash();

        // Close target: slam
        private bool CanSlam() =>
            Time.time >= _slamReadyTime && _config.slamStages != null && _config.slamStages.Length > 0
            && IsValidTarget(_target) && FlatDistance(_target.position) <= _config.slamTriggerRange;

        // Target in dash range (EnemyStats attack range)
        private bool CanDash() => _attackCooldownTimer <= 0f && base.IsPlayerInAttackRange();

        // An attack is locked in once it starts, losing the target doesn't stop it
        public override void ClearTarget()
        {
            if (IsAttacking)
            {
                _target = null;
                return;
            }
            base.ClearTarget();
        }

        // Server only, called by the state machine
        public override void PerformAttackRpc()
        {
            if (!IsServer || _isDead || IsAttacking || _target == null) return;

            if (CanSlam()) StartAttack(Attack.Slam, SlamRoutine());
            else if (CanDash()) StartAttack(Attack.Dash, DashAttackRoutine(_target));
        }

        private void StartAttack(Attack attack, IEnumerator routine)
        {
            _currentAttack = attack;
            _attackRoutine = StartCoroutine(routine);
        }

        // Dash

        private IEnumerator DashAttackRoutine(Transform target)
        {
            _hitPlayers.Clear();

            yield return TurnTowards(target, _config.dashTurnDuration);

            Vector3 start = transform.position;
            Vector3 direction = Flat(transform.forward).normalized;
            float dashLength = GetDashLength(start, direction);
            float hitLength = dashLength + _config.dashReach;

            ShowDashTelegraphRpc(start, start + direction * hitLength, NetworkManager.ServerTime.Time);

            yield return new WaitForSeconds(_config.dashTelegraphDuration);

            PlayAttackAnimationRpc();
            PlaySoundForEveryone(_config.dashSound, transform.position);
            PlayTelegraphImpactRpc(0);

            float speed = dashLength / _config.dashDuration;
            float traveled = 0f;
            while (traveled < dashLength && !IsRooted)
            {
                float step = Mathf.Min(speed * Time.deltaTime, dashLength - traveled);
                MoveBy(direction * step);
                traveled += step;

                HitPlayersInStrip(start, direction, traveled + _config.dashReach);
                yield return null;
            }
            HitPlayersInStrip(start, direction, traveled + _config.dashReach);

            yield return new WaitForSeconds(_config.dashRecoveryDuration);

            _attackCooldownTimer = _stats.attackCooldown;
            FinishAttack();
        }

        // Turns to the target, following it while it moves
        private IEnumerator TurnTowards(Transform target, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (target != null) RotateTowards(target.position, elapsed / duration);
                yield return null;
            }
            if (target != null) RotateTowards(target.position, 1f);
        }

        private void RotateTowards(Vector3 point, float t)
        {
            Vector3 direction = Flat(point - transform.position);
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), t);
        }

        // Stops before walls and edges of the walkable ground
        private float GetDashLength(Vector3 start, Vector3 direction)
        {
            Vector3 end = start + direction * _config.dashDistance;
            if (NavMesh.Raycast(start, end, out NavMeshHit hit, NavMesh.AllAreas))
                return Mathf.Max(0f, hit.distance - WallPadding);
            return _config.dashDistance;
        }

        private void MoveBy(Vector3 offset)
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.Move(offset);
            else transform.position += offset;
        }

        // Everyone between the start and the front of the tank, once per dash
        private void HitPlayersInStrip(Vector3 start, Vector3 direction, float length)
        {
            if (length <= 0f) return;

            Vector3 center = start + direction * (length * 0.5f) + Vector3.up * (HitBoxHeight * 0.5f - 0.5f);
            Vector3 halfExtents = new Vector3(_config.dashWidth * 0.5f, HitBoxHeight * 0.5f, length * 0.5f);

            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, s_overlapHits, Quaternion.LookRotation(direction), _playerLayer);
            for (int i = 0; i < count; i++)
            {
                PlayerCombat player = s_overlapHits[i].GetComponentInParent<PlayerCombat>();
                if (player == null || !player.IsTargetable || !_hitPlayers.Add(player.NetworkObjectId)) continue;

                player.TakeDamage(RollAttackDamage(), _stats.damageType);
            }
        }

        // Ground slam

        // One circle after another, from the small one to the big one
        private IEnumerator SlamRoutine()
        {
            Vector3 center = transform.position;

            yield return new WaitForSeconds(_config.slamStartupDuration);

            for (int i = 0; i < _config.slamStages.Length; i++)
            {
                SlamStage stage = _config.slamStages[i];
                ShowCircleTelegraphRpc(i, center, stage.radius, NetworkManager.ServerTime.Time, stage.telegraphDuration);

                yield return new WaitForSeconds(stage.telegraphDuration);

                PlayAttackAnimationRpc();
                PlaySoundForEveryone(_config.slamSound, center);
                PlayTelegraphImpactRpc(i);
                HitPlayersInCircle(center, stage);
            }

            yield return new WaitForSeconds(_config.slamRecoveryDuration);

            _slamReadyTime = Time.time + _config.slamCooldown;
            FinishAttack();
        }

        // Everyone inside the circle (feet within the height tolerance)
        private void HitPlayersInCircle(Vector3 center, SlamStage stage)
        {
            float tolerance = _config.slamVerticalTolerance;
            float sqrRadius = stage.radius * stage.radius;
            _hitPlayers.Clear();

            int count = Physics.OverlapSphereNonAlloc(center, stage.radius + tolerance, s_overlapHits, _playerLayer);
            for (int i = 0; i < count; i++)
            {
                PlayerCombat player = s_overlapHits[i].GetComponentInParent<PlayerCombat>();
                if (player == null || !player.IsTargetable) continue;

                Vector3 offset = player.transform.position - center;
                if (Mathf.Abs(offset.y) > tolerance) continue;
                offset.y = 0f;
                if (offset.sqrMagnitude > sqrRadius || !_hitPlayers.Add(player.NetworkObjectId)) continue;

                player.TakeDamage(Random.Range(stage.minDamage, stage.maxDamage), _config.slamDamageType);
                if (stage.stunDuration > 0f && player.TryGetComponent(out PlayerStun stun)) stun.ApplyStun(stage.stunDuration);
            }
        }

        // Cancel & death

        // Stun from players stops the attack
        public override void CancelAttack()
        {
            base.CancelAttack();
            if (!IsServer || !IsAttacking) return;

            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            if (_currentAttack == Attack.Dash) _attackCooldownTimer = _stats.attackCooldown * 0.5f;
            else _slamReadyTime = Time.time + _config.slamCooldown * 0.5f;

            HideTelegraphsRpc();
            FinishAttack();
        }

        private void FinishAttack()
        {
            _currentAttack = Attack.None;
            _attackRoutine = null;
        }

        protected override void Die(NetworkObjectReference attackerRef)
        {
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            if (IsAttacking) HideTelegraphsRpc();
            FinishAttack();

            PlaySoundForEveryone(_config.deathSound, transform.position);
            base.Die(attackerRef);
        }

        // Ground warnings (clients and host)

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ShowDashTelegraphRpc(Vector3 start, Vector3 end, double startServerTime)
        {
            GetTelegraph(0).ShowStrip(start, end, _config.dashWidth, startServerTime, _config.dashTelegraphDuration,
                                      _config.telegraphColor, _config.telegraphWarningColor,
                                      _config.warningThreshold, TelegraphGroundMask);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ShowCircleTelegraphRpc(int index, Vector3 center, float radius, double startServerTime, float duration)
        {
            GetTelegraph(index).ShowCircle(center, radius, startServerTime, duration,
                                           _config.telegraphColor, _config.telegraphWarningColor,
                                           _config.warningThreshold, TelegraphGroundMask);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayTelegraphImpactRpc(int index)
        {
            if (index < _telegraphs.Count) _telegraphs[index].PlayImpact(_config.impactFadeDuration);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void HideTelegraphsRpc()
        {
            foreach (SpectreTankTelegraph telegraph in _telegraphs) telegraph.Hide();
        }

        // Players are never ground
        private int TelegraphGroundMask => _config.groundMask & ~_playerLayer;

        // One warning per circle, so the last one can fade while the next one grows
        private SpectreTankTelegraph GetTelegraph(int index)
        {
            while (_telegraphs.Count <= index) _telegraphs.Add(SpectreTankTelegraph.Create(_config.telegraphMaterial));
            return _telegraphs[index];
        }

        private void DestroyTelegraphs()
        {
            foreach (SpectreTankTelegraph telegraph in _telegraphs)
            {
                if (telegraph != null) Destroy(telegraph.gameObject);
            }
            _telegraphs.Clear();
        }

        // Helpers

        private float FlatDistance(Vector3 position) => Flat(position - transform.position).magnitude;

        private static Vector3 Flat(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        // Gizmos

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (_config == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            float length = _config.dashDistance + _config.dashReach;
            Gizmos.DrawWireCube(new Vector3(0f, 0.1f, length * 0.5f), new Vector3(_config.dashWidth, 0.2f, length));
            Gizmos.matrix = Matrix4x4.identity;

            if (_config.slamStages == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.1f);
            foreach (SlamStage stage in _config.slamStages)
            {
                if (stage != null) Gizmos.DrawWireSphere(transform.position, stage.radius);
            }
        }
    }
}
