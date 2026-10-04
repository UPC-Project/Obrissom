using System.Collections;
using System.Collections.Generic;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Picozapato: plunges its rooted hand into the ground and drops a telegraphed circle on a player.
    /// Punishes players that get too close with a quick basic attack.
    /// All decisions and damage run on the server through the shared EnemyStateMachine.
    /// </summary>
    [RequireComponent(typeof(PicozapatoAnimation))]
    public class PicozapatoEnemy : EnemyBase
    {
        private const int MaxOverlapHits = 32;

        [Header("Picozapato")]
        [SerializeField] private PicozapatoConfig _config;

        private static readonly Collider[] s_overlapHits = new Collider[MaxOverlapHits];
        private static readonly HashSet<ulong> s_hitPlayers = new HashSet<ulong>();

        // Server — combat
        private bool _isAttacking;
        private bool _wasStaggered;
        private float _basicReadyTime;
        private Coroutine _attackRoutine;

        // Server — roaming
        private Vector3 _home;
        private float _roamResumeTime;
        private bool _isRoamPausing;

        // Rotation is locked during the basic attack windup so the hit is dodgeable
        public override bool CanFaceTarget => !_isAttacking;

        // Lifecycle

        protected override void Awake()
        {
            base.Awake();
            if (_config == null) Debug.LogError($"[Picozapato] {name} has no PicozapatoConfig assigned.", this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) return;

            _home = transform.position;
            ConfigureAgent();
        }

        private void ConfigureAgent()
        {
            _agent.acceleration = _config.acceleration;
            _agent.angularSpeed = _config.angularSpeed;
            _agent.stoppingDistance = _config.stoppingDistance;
            _agent.obstacleAvoidanceType = _config.obstacleAvoidance;
            _agent.avoidancePriority = Random.Range(_config.avoidancePriorityRange.x, _config.avoidancePriorityRange.y + 1);
        }

        // Roaming — random points around the spawn (or the patrol points if any), with pauses

        public override void OnMoveStateEnter()
        {
            _isRoamPausing = false;
            PickRoamDestination();
        }

        public override void OnMoveStateTick()
        {
            if (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.1f) return;

            if (!_isRoamPausing)
            {
                _isRoamPausing = true;
                _roamResumeTime = Time.time + Random.Range(_config.roamPause.x, _config.roamPause.y);
                return;
            }

            if (Time.time < _roamResumeTime) return;

            _isRoamPausing = false;
            PickRoamDestination();
        }

        private void PickRoamDestination()
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (NavMesh.SamplePosition(GetRoamCandidate(), out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        private Vector3 GetRoamCandidate()
        {
            Vector2 random = Random.insideUnitCircle;

            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                GameObject point = _patrolPoints[Random.Range(0, _patrolPoints.Length)];
                if (point != null) return point.transform.position + new Vector3(random.x, 0f, random.y) * 1.5f;
            }

            return _home + new Vector3(random.x, 0f, random.y) * _config.roamRadius;
        }

        // Chase — stop just inside attack range instead of walking into the player

        public override void OnChaseStateTick()
        {
            if (_target == null) return;

            Vector3 fromTarget = transform.position - _target.position;
            fromTarget.y = 0f;
            Vector3 destination = fromTarget.sqrMagnitude > 0.01f
                ? _target.position + fromTarget.normalized * (_stats.attackRange * 0.7f)
                : _target.position;

            _agent.SetDestination(destination);
        }

        // Combat — decision

        /// <summary>True while an attack is running (it is committed), or if one can start right now.</summary>
        public override bool IsPlayerInAttackRange()
        {
            if (_isAttacking) return true;
            return _target != null && Time.time >= _basicReadyTime && FlatDistance(_target.position) <= _stats.attackRange;
        }

        /// <summary>
        /// Called by the state machine every eval tick while in Attack.
        /// Server only. Not an RPC on Picozapato: attacks are decided by the server AI, clients can't trigger them.
        /// </summary>
        public override void PerformAttackRpc()
        {
            if (!IsServer || _isDead || _isAttacking || Time.time < _basicReadyTime) return;
            _attackRoutine = StartCoroutine(BasicAttackRoutine());
        }

        // Combat — basic attack

        private IEnumerator BasicAttackRoutine()
        {
            _isAttacking = true;
            _wasStaggered = false;
            PlayAttackAnimationRpc();

            yield return new WaitForSeconds(_config.basicWindupDuration);

            bool interrupted = _wasStaggered || _target == null;
            if (!interrupted)
            {
                PlaySoundForEveryone(_config.basicAttackSound, transform.position);
                ResolveBasicHit();
                yield return new WaitForSeconds(_config.basicRecoveryDuration);
            }

            _basicReadyTime = Time.time + _config.GetRandomizedCooldown(_stats.attackCooldown);
            _isAttacking = false;
            _attackRoutine = null;
        }

        private void ResolveBasicHit()
        {
            float halfAngle = _config.basicAttackAngle * 0.5f;
            Vector3 origin = transform.position;
            s_hitPlayers.Clear();

            int count = Physics.OverlapSphereNonAlloc(origin, _stats.attackRange, s_overlapHits, _playerLayer);
            for (int i = 0; i < count; i++)
            {
                PlayerCombat player = s_overlapHits[i].GetComponentInParent<PlayerCombat>();

                // One hit per player even if it has several colliders
                if (player == null || !s_hitPlayers.Add(player.NetworkObjectId)) continue;

                Vector3 toPlayer = player.transform.position - origin;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, toPlayer) > halfAngle) continue;

                player.TakeDamage(RollAttackDamage(), _stats.damageType);
            }
        }

        // Combat — lifecycle

        protected override void OnTakeDamage(float rawAmount)
        {
            if (_isAttacking) _wasStaggered = true;
        }

        protected override void Die(NetworkObjectReference attackerRef)
        {
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _isAttacking = false;

            PlaySoundForEveryone(_config.deathSound, transform.position);
            base.Die(attackerRef);
        }

        // Helpers

        private float FlatDistance(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        // Gizmos

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (_config == null) return;

            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(Application.isPlaying ? _home : transform.position, _config.roamRadius);
        }
    }
}
