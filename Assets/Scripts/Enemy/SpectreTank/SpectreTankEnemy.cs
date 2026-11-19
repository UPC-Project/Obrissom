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
    /// Slow tank. Marks a strip in front of it, then dashes along it and hits everyone inside.
    /// The server runs the AI. Clients only draw the ground warning.
    /// </summary>
    public class SpectreTankEnemy : EnemyBase
    {
        private const int MaxOverlapHits = 16;
        private const float HitBoxHeight = 3f;
        private const float WallPadding = 0.3f;

        [Header("Spectre Tank")]
        [SerializeField] private SpectreTankConfig _config;

        private static readonly Collider[] s_overlapHits = new Collider[MaxOverlapHits];

        private readonly HashSet<ulong> _hitPlayers = new HashSet<ulong>();
        private Coroutine _attackRoutine;
        private bool _isAttacking;
        private SpectreTankTelegraph _telegraph;

        // Hits don't stop an attack, and it doesn't turn once the strip is shown
        protected override bool IsStaggerImmune => _isAttacking;
        public override bool CanFaceTarget => !_isAttacking;

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
            DestroyTelegraph();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            DestroyTelegraph();
            base.OnDestroy();
        }

        private void ConfigureAgent()
        {
            _agent.acceleration = _config.acceleration;
            _agent.angularSpeed = _config.angularSpeed;
            _agent.stoppingDistance = _config.stoppingDistance;
        }

        // Attack decision

        // In dash range and off cooldown (or already attacking)
        public override bool IsPlayerInAttackRange()
        {
            if (_isAttacking) return true;
            return _attackCooldownTimer <= 0f && base.IsPlayerInAttackRange();
        }

        // The dash is locked in once it starts, losing the target doesn't stop it
        public override void ClearTarget()
        {
            if (_isAttacking)
            {
                _target = null;
                return;
            }
            base.ClearTarget();
        }

        // Server only, called by the state machine
        public override void PerformAttackRpc()
        {
            if (!IsServer || _isDead || _isAttacking || _target == null || _attackCooldownTimer > 0f) return;

            _attackRoutine = StartCoroutine(DashAttackRoutine(_target));
        }

        // Dash

        private IEnumerator DashAttackRoutine(Transform target)
        {
            _isAttacking = true;
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
            PlayTelegraphImpactRpc();

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

        // Cancel & death

        // Stun from players stops the attack
        public override void CancelAttack()
        {
            base.CancelAttack();
            if (!IsServer || !_isAttacking) return;

            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackCooldownTimer = _stats.attackCooldown * 0.5f;
            HideTelegraphRpc();
            FinishAttack();
        }

        private void FinishAttack()
        {
            _isAttacking = false;
            _attackRoutine = null;
        }

        protected override void Die(NetworkObjectReference attackerRef)
        {
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            if (_isAttacking) HideTelegraphRpc();
            FinishAttack();

            PlaySoundForEveryone(_config.deathSound, transform.position);
            base.Die(attackerRef);
        }

        // Ground warning (clients and host)

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ShowDashTelegraphRpc(Vector3 start, Vector3 end, double startServerTime)
        {
            GetTelegraph().ShowStrip(start, end, _config.dashWidth, startServerTime, _config.dashTelegraphDuration,
                                     _config.telegraphColor, _config.telegraphWarningColor,
                                     _config.warningThreshold, TelegraphGroundMask);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayTelegraphImpactRpc()
        {
            if (_telegraph != null) _telegraph.PlayImpact(_config.impactFadeDuration);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void HideTelegraphRpc()
        {
            if (_telegraph != null) _telegraph.Hide();
        }

        // Players are never ground
        private int TelegraphGroundMask => _config.groundMask & ~_playerLayer;

        private SpectreTankTelegraph GetTelegraph()
        {
            if (_telegraph == null) _telegraph = SpectreTankTelegraph.Create(_config.telegraphMaterial);
            return _telegraph;
        }

        private void DestroyTelegraph()
        {
            if (_telegraph != null) Destroy(_telegraph.gameObject);
            _telegraph = null;
        }

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
        }
    }
}
