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
    /// Picozapato: plunges its rooted hand into the ground and drops a telegraphed circle on a player.
    /// Punishes players that get too close with a quick basic attack.
    /// All decisions and damage run on the server through the shared EnemyStateMachine.
    /// Clients render the area attack from a single replicated snapshot (animations, ground indicator, sounds).
    ///
    /// Area attack: Startup (hand into the ground) - target position locked, circle telegraph  - impact (damage) - recovery.
    /// </summary>
    [RequireComponent(typeof(PicozapatoAnimation))]
    public class PicozapatoEnemy : EnemyBase, IHerdMember
    {
        private const int MaxOverlapHits = 32;

        [Header("Picozapato")]
        [SerializeField] private PicozapatoConfig _config;

        [Tooltip("Optional, for Picozapatos placed in a scene. Spawners assign the herd at runtime. " +
                 "Without one, it roams alone around its spawn point.")]
        [SerializeField] private EnemyHerd _herd;

        private static readonly Collider[] s_overlapHits = new Collider[MaxOverlapHits];
        private static readonly HashSet<ulong> s_hitPlayers = new HashSet<ulong>();

        // Replicated — area attack state for clients
        private readonly NetworkVariable<PicozapatoAttackSnapshot> _attackSnapshot =
            new NetworkVariable<PicozapatoAttackSnapshot>();

        private PicozapatoAnimation _picozapatoAnimation;
        private PicozapatoAreaIndicator _indicator;

        // Server — combat
        private PicozapatoAttackKind _pendingAttack;
        private PicozapatoAttackKind _currentAttack;
        private Transform _attackTarget;
        private Coroutine _attackRoutine;
        private bool _isRooted;
        private bool _wasStaggered;
        private float _basicReadyTime;
        private float _areaReadyTime;
        private byte _attackSequence;

        // Server — individual variation, rolled once on spawn
        private float _speedMultiplier = 1f;

        // Server — roaming
        private bool _ownsHerd;
        private bool _isRoamPausing;
        private float _roamResumeTime;

        private bool IsAttacking => _currentAttack != PicozapatoAttackKind.None;

        protected override bool IsStaggerImmune => _currentAttack == PicozapatoAttackKind.Area;

        public override bool CanFaceTarget => !_isRooted && _currentAttack != PicozapatoAttackKind.Basic;

        // IHerdMember

        public float HerdSpacing => _config.herdSpacing;

        // Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _picozapatoAnimation = GetComponent<PicozapatoAnimation>();
            if (_config == null) Debug.LogError($"[Picozapato] {name} has no PicozapatoConfig assigned.", this);
        }

        public override void OnNetworkSpawn()
        {
            // Before base: the state machine starts inside base.OnNetworkSpawn and already reads the speed variation
            if (IsServer) RollIndividualVariation();

            base.OnNetworkSpawn();

            _attackSnapshot.OnValueChanged += OnAttackSnapshotChanged;
            PresentSnapshot(_attackSnapshot.Value, isLive: false); // Late joiners: render an attack already in progress

            if (!IsServer) return;

            ConfigureAgent();
            EnemySteering.Register(this);

            if (_herd != null)
            {
                EnemyHerd sceneHerd = _herd;
                _herd = null;
                sceneHerd.AddMember(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            _attackSnapshot.OnValueChanged -= OnAttackSnapshotChanged;
            DestroyIndicator();

            if (IsServer) LeaveGroupSystems();

            base.OnNetworkDespawn();
        }

        private void RollIndividualVariation()
        {
            _speedMultiplier = 1f + Random.Range(-_config.speedVariance, _config.speedVariance);

            // A group spawned together must not open with a synchronized volley
            _areaReadyTime = Time.time + Random.Range(0f, _config.areaCooldown);
        }

        private void LeaveGroupSystems()
        {
            EnemySteering.Unregister(this);

            if (_herd != null)
            {
                EnemyHerd herd = _herd;
                _herd = null;
                herd.RemoveMember(this);
            }
        }

        public override void OnDestroy()
        {
            DestroyIndicator();
            base.OnDestroy();
        }

        private void ConfigureAgent()
        {
            _agent.acceleration = _config.acceleration;
            _agent.angularSpeed = _config.angularSpeed;
            _agent.stoppingDistance = _config.stoppingDistance;
            _agent.obstacleAvoidanceType = _config.obstacleAvoidance;
            _agent.avoidancePriority = Random.Range(_config.avoidancePriorityRange.x, _config.avoidancePriorityRange.y + 1);
        }

        public override float GetMoveSpeed(EnemyState state) => base.GetMoveSpeed(state) * _speedMultiplier;

        public override void OnStateChanged(EnemyState previous, EnemyState current)
        {
            // Leaving roaming (combat, death...): free the destination so another member can use that spot
            if (previous == EnemyState.Move && _herd != null) _herd.ReleaseDestination(this);
        }

        // Roaming — each member wanders on its own inside the herd area (a solo Picozapato is a herd of one)

        public void SetHerd(EnemyHerd herd)
        {
            if (_herd == herd) return;

            EnemyHerd previous = _herd;
            _herd = herd;
            _ownsHerd = false;

            if (previous != null) previous.RemoveMember(this); // Auto-created herds destroy themselves when empty
        }

        public override void SetPatrolPoints(GameObject[] points)
        {
            base.SetPatrolPoints(points);
            if (_ownsHerd && _herd != null) _herd.SetWaypoints(points);
        }

        public override void OnMoveStateEnter()
        {
            EnsureHerd();

            // Short random wait before the first leg so members spawned together don't all leave at once
            _isRoamPausing = true;
            _roamResumeTime = Time.time + Random.Range(0f, _herd.PauseAtDestination.x);
        }

        public override void OnMoveStateTick()
        {
            EnsureHerd();

            if (_isRoamPausing)
            {
                if (Time.time >= _roamResumeTime) StartRoamLeg();
                else KeepPersonalSpace();
                return;
            }

            bool arrived = !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
            if (!arrived) return;

            Vector2 pause = _herd.PauseAtDestination;
            _isRoamPausing = true;
            _roamResumeTime = Time.time + Random.Range(pause.x, pause.y);
        }

        private void EnsureHerd()
        {
            if (_herd != null) return;

            var herdObject = new GameObject($"{name} Herd");
            herdObject.transform.position = transform.position;
            var herd = herdObject.AddComponent<EnemyHerd>();
            herd.Configure(_config.roamRadius, _config.roamPause, _patrolPoints, destroyWhenEmpty: true);
            herd.AddMember(this);
            _ownsHerd = true;
        }

        private void StartRoamLeg()
        {
            _isRoamPausing = false;
            _agent.speed = GetMoveSpeed(EnemyState.Move);

            // The herd picks a spot away from the other members' destinations and positions
            if (_herd.TryGetDestination(this, out Vector3 destination))
            {
                _agent.SetDestination(destination);
            }
            else
            {
                // No valid spot this time: wait a bit and try again
                _isRoamPausing = true;
                _roamResumeTime = Time.time + 1f;
            }
        }

        // While standing still, step aside if another enemy is too close. While walking, NavMesh avoidance handles it.
        private void KeepPersonalSpace()
        {
            if (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.1f) return;

            Vector3 push = EnemySteering.ComputeSeparation(this, _config.separationRadius);
            if (push.sqrMagnitude < 0.01f) return;

            Vector3 target = transform.position + push.normalized * _config.separationStrength;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
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
            PublishSnapshot(PicozapatoAttackPhase.Windup, transform.position);

            yield return new WaitForSeconds(_config.areaStartupDuration);

            if (!IsValidTarget(_attackTarget))
            {
                // Target gone during startup: abort with a short cooldown
                ClearSnapshot();
                _areaReadyTime = Time.time + _config.areaCooldown * 0.25f;
                FinishAttack();
                yield break;
            }

            // Locked here: the circle never follows the player, so it is always dodgeable
            Vector3 center = GetGroundPoint(_attackTarget.position);
            _isRooted = true;
            PublishSnapshot(PicozapatoAttackPhase.Telegraph, center);

            yield return new WaitForSeconds(_config.areaTelegraphDuration);

            ResolveAreaHit(center);
            PublishSnapshot(PicozapatoAttackPhase.Impact, center);

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
            // Killing it mid-cast cancels the attack and removes the circle
            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            FinishAttack();
            _pendingAttack = PicozapatoAttackKind.None;
            if (_attackSnapshot.Value.Kind != PicozapatoAttackKind.None) ClearSnapshot();

            PlaySoundForEveryone(_config.deathSound, transform.position);
            base.Die(attackerRef);
            LeaveGroupSystems();
        }

        // Replication — server side

        private void PublishSnapshot(PicozapatoAttackPhase phase, Vector3 center)
        {
            if (phase == PicozapatoAttackPhase.Windup) _attackSequence++;

            _attackSnapshot.Value = new PicozapatoAttackSnapshot
            {
                Sequence = _attackSequence,
                Kind = PicozapatoAttackKind.Area,
                Phase = phase,
                Center = center,
                PhaseStartTime = NetworkManager.ServerTime.Time
            };
        }

        private void ClearSnapshot()
        {
            _attackSnapshot.Value = new PicozapatoAttackSnapshot
            {
                Sequence = _attackSequence,
                Kind = PicozapatoAttackKind.None,
                PhaseStartTime = NetworkManager.ServerTime.Time
            };
        }

        // Replication — presentation (host and clients)

        private void OnAttackSnapshotChanged(PicozapatoAttackSnapshot previous, PicozapatoAttackSnapshot current) =>
            PresentSnapshot(current, isLive: true);

        private void PresentSnapshot(PicozapatoAttackSnapshot snapshot, bool isLive)
        {
            if (!IsClient) return; // Dedicated server: nothing to render

            if (snapshot.Kind != PicozapatoAttackKind.Area)
            {
                HideIndicator();
                _picozapatoAnimation.SetRooted(false);
                return;
            }

            double elapsed = NetworkManager.ServerTime.Time - snapshot.PhaseStartTime;

            switch (snapshot.Phase)
            {
                case PicozapatoAttackPhase.Windup:
                    if (!isLive) break;
                    _picozapatoAnimation.PlayAreaWindup();
                    PlayLocalSound(_config.areaWindupSound, transform.position);
                    break;

                case PicozapatoAttackPhase.Telegraph:
                    _picozapatoAnimation.SetRooted(true);
                    if (elapsed >= _config.areaTelegraphDuration) break;


                    GetIndicator().ShowTelegraph(snapshot.Center, _config.areaRadius, snapshot.PhaseStartTime,
                                                 _config.areaTelegraphDuration, _config.indicatorColor,
                                                 _config.indicatorWarningColor, IndicatorGroundMask);
                    break;

                case PicozapatoAttackPhase.Impact:
                    _picozapatoAnimation.SetRooted(false);
                    float remaining = _config.indicatorImpactDuration - (float)elapsed;
                    if (remaining <= 0f)
                    {
                        HideIndicator();
                        break;
                    }

                    GetIndicator().PlayImpact(snapshot.Center, _config.areaRadius, remaining,
                                              _config.indicatorWarningColor, IndicatorGroundMask);
                    if (!isLive) break;
                    _picozapatoAnimation.PlayAreaImpact();
                    PlayLocalSound(_config.areaImpactSound, snapshot.Center);
                    break;
            }
        }

        // Players are never ground, even if the configured mask includes their layer
        private int IndicatorGroundMask => _config.groundMask & ~_playerLayer;

        private PicozapatoAreaIndicator GetIndicator()
        {
            if (_indicator == null)
            {
                _indicator = _config.areaIndicatorPrefab != null
                    ? Instantiate(_config.areaIndicatorPrefab)
                    : PicozapatoAreaIndicator.CreateFallback(_config.fallbackIndicatorMaterial);
            }
            return _indicator;
        }

        private void HideIndicator()
        {
            if (_indicator != null) _indicator.Hide();
        }

        private void DestroyIndicator()
        {
            if (_indicator != null) Destroy(_indicator.gameObject);
            _indicator = null;
        }

        // Sounds
        private static void PlayLocalSound(AudioID id, Vector3 position)
        {
            if (id != AudioID.None && AudioManager.Instance != null) AudioManager.Instance.PlaySound(id, position);
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

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (_config == null) return;

            // In play mode the herd draws its own area and slots
            if (!Application.isPlaying && _herd == null)
            {
                Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
                Gizmos.DrawWireSphere(transform.position, _config.roamRadius);
            }

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, _config.areaMaxRange);
        }
    }
}
