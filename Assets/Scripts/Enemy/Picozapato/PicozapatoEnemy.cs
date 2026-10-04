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
    ///
    /// Area attack: Startup (hand into the ground) - target position locked, circle telegraph  - impact (damage) - recovery.
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
        private PicozapatoAttackKind _pendingAttack;
        private PicozapatoAttackKind _currentAttack;
        private Transform _attackTarget;
        private Coroutine _attackRoutine;
        private bool _isRooted;
        private bool _wasStaggered;
        private float _basicReadyTime;
        private float _areaReadyTime;

        // Server — area debug (gizmos until the client indicator exists)
        private Vector3 _areaCenter;
        private float _areaTelegraphStartTime;

        // Server — roaming
        private Vector3 _home;
        private float _roamResumeTime;
        private bool _isRoamPausing;

        private bool IsAttacking => _currentAttack != PicozapatoAttackKind.None;

        // The rooted cast always resolves once started: killing the Picozapato is the counterplay, not stunlocking it
        protected override bool IsStaggerImmune => _currentAttack == PicozapatoAttackKind.Area;

        // Rotation is locked while the hand is in the ground and during the basic attack windup, so both are dodgeable
        public override bool CanFaceTarget => !_isRooted && _currentAttack != PicozapatoAttackKind.Basic;

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

            // A group spawned together must not open with a synchronized volley
            _areaReadyTime = Time.time + Random.Range(0f, _config.areaCooldown);

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

        /// <summary>
        /// True while an attack is reserved or running (the attack is committed), or if one can start right now.
        /// The basic attack has priority in its range, the area attack covers mid range.
        /// </summary>
        public override bool IsPlayerInAttackRange()
        {
            if (IsAttacking || _pendingAttack != PicozapatoAttackKind.None) return true;
            if (_target == null) return false;

            float distance = FlatDistance(_target.position);

            if (distance <= _stats.attackRange && Time.time >= _basicReadyTime)
                _pendingAttack = PicozapatoAttackKind.Basic;
            else if (distance >= _config.areaMinRange && distance <= _config.areaMaxRange && Time.time >= _areaReadyTime)
                _pendingAttack = PicozapatoAttackKind.Area;

            if (_pendingAttack == PicozapatoAttackKind.None) return false;

            _attackTarget = _target;
            return true;
        }

        /// <summary>
        /// Called by the state machine every eval tick while in Attack. Starts the reserved attack.
        /// Server only. Not an RPC on Picozapato: attacks are decided by the server AI, clients can't trigger them.
        /// </summary>
        public override void PerformAttackRpc()
        {
            if (!IsServer || _isDead || IsAttacking || _pendingAttack == PicozapatoAttackKind.None) return;

            _currentAttack = _pendingAttack;
            _pendingAttack = PicozapatoAttackKind.None;
            _attackRoutine = StartCoroutine(_currentAttack == PicozapatoAttackKind.Basic
                ? BasicAttackRoutine()
                : AreaAttackRoutine());
        }

        // Combat — basic attack

        private IEnumerator BasicAttackRoutine()
        {
            _wasStaggered = false;
            PlayAttackAnimationRpc();

            yield return new WaitForSeconds(_config.basicWindupDuration);

            bool interrupted = _wasStaggered || _attackTarget == null;
            if (!interrupted)
            {
                PlaySoundForEveryone(_config.basicAttackSound, transform.position);
                ResolveBasicHit();
                yield return new WaitForSeconds(_config.basicRecoveryDuration);
            }

            _basicReadyTime = Time.time + _config.GetRandomizedCooldown(_stats.attackCooldown);
            FinishAttack();
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

        // Combat — area attack

        private IEnumerator AreaAttackRoutine()
        {
            PlayAttackAnimationRpc();

            yield return new WaitForSeconds(_config.areaStartupDuration);

            if (!IsValidTarget(_attackTarget))
            {
                // Target gone during startup: abort with a short cooldown
                _areaReadyTime = Time.time + _config.areaCooldown * 0.25f;
                FinishAttack();
                yield break;
            }

            // Locked here: the circle never follows the player, so it is always dodgeable
            _areaCenter = GetGroundPoint(_attackTarget.position);
            _areaTelegraphStartTime = Time.time;
            _isRooted = true;

            yield return new WaitForSeconds(_config.areaTelegraphDuration);

            ResolveAreaHit(_areaCenter);

            yield return new WaitForSeconds(_config.areaRecoveryDuration);

            _areaReadyTime = Time.time + _config.GetRandomizedCooldown(_config.areaCooldown);
            FinishAttack();
        }

        private void ResolveAreaHit(Vector3 center)
        {
            float sqrRadius = _config.areaRadius * _config.areaRadius;
            float broadRadius = _config.areaRadius + _config.areaVerticalTolerance;
            s_hitPlayers.Clear();

            int count = Physics.OverlapSphereNonAlloc(center, broadRadius, s_overlapHits, _playerLayer);
            for (int i = 0; i < count; i++)
            {
                PlayerCombat player = s_overlapHits[i].GetComponentInParent<PlayerCombat>();
                if (player == null || !s_hitPlayers.Add(player.NetworkObjectId)) continue;

                // Exact test on the player's feet: what the circle shows is what hits
                Vector3 offset = player.transform.position - center;
                if (Mathf.Abs(offset.y) > _config.areaVerticalTolerance) continue;
                offset.y = 0f;
                if (offset.sqrMagnitude > sqrRadius) continue;

                player.TakeDamage(Random.Range(_config.areaMinDamage, _config.areaMaxDamage), _config.areaDamageType);
            }
        }

        private static Vector3 GetGroundPoint(Vector3 position) =>
            NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : position;

        // Combat — lifecycle

        protected override void OnTakeDamage(float rawAmount)
        {
            if (_currentAttack == PicozapatoAttackKind.Basic) _wasStaggered = true;
        }

        private void FinishAttack()
        {
            _currentAttack = PicozapatoAttackKind.None;
            _attackRoutine = null;
            _attackTarget = null;
            _isRooted = false;
        }

        protected override void Die(NetworkObjectReference attackerRef)
        {
            // Killing it mid-cast cancels the attack
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            FinishAttack();
            _pendingAttack = PicozapatoAttackKind.None;

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

        private static bool IsValidTarget(Transform target) =>
            target != null
            && target.gameObject.activeInHierarchy
            && target.TryGetComponent(out PlayerCombat player)
            && player._health.Value > 0f;

        // Gizmos

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_isRooted || _currentAttack != PicozapatoAttackKind.Area || _config == null) return;

            float progress = Mathf.Clamp01((Time.time - _areaTelegraphStartTime) / _config.areaTelegraphDuration);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(_areaCenter, Quaternion.identity, new Vector3(1f, 0.02f, 1f));

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, _config.areaRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawSphere(Vector3.zero, _config.areaRadius * progress);

            Gizmos.matrix = previous;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (_config == null) return;

            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(Application.isPlaying ? _home : transform.position, _config.roamRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, _config.areaMaxRange);
        }
    }
}
