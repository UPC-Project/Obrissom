using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Abstract base class for all enemies.
    /// Do not attach directly to a prefab — use concrete classes.
    /// Requires EnemyStats ScriptableObject assigned in the Inspector.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyAnimation))]
    [RequireComponent(typeof(ItemDropper))]
    [RequireComponent(typeof(EnemyStateMachine))]
    [RequireComponent(typeof(EnemyDamagePopUp))]
    public abstract class EnemyBase : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] protected EnemyStats _stats;

        [Header("Detection")]
        [SerializeField] protected LayerMask _playerLayer;

        [Header("Patrol")]
        [SerializeField] protected GameObject[] _patrolPoints;
        [SerializeField] private float _waypointPauseDuration = 1f;
        [SerializeField] private float _wanderRadius = 1.5f;

        public Transform Target => _target;

        public EnemyStats Stats => _stats;

        // Components
        protected NavMeshAgent _agent;
        protected EnemyAnimation _enemyAnimation;
        protected ItemDropper _itemDropper;
        protected EnemyDamagePopUp _damagePopUp;

        // Runtime state
        protected float _currentHealth;
        protected float _attackCooldownTimer;
        protected bool _isDead;
        protected int _currentPatrolIndex;
        protected Transform _target;
        private bool _isWaypointPausing;

        // Lifecycle


        protected EnemyStateMachine _stateMachine;


        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _enemyAnimation = GetComponent<EnemyAnimation>();
            _itemDropper = GetComponent<ItemDropper>();
            _stateMachine = GetComponent<EnemyStateMachine>();
            _damagePopUp = GetComponent<EnemyDamagePopUp>();



        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            _currentHealth = _stats.maxHealth;
            _agent.speed = _stats.moveSpeed;

            _stateMachine.Initialize(this, _agent);
        }

        protected virtual void Update()
        {
            if (!IsServer || _isDead) return;

            _attackCooldownTimer -= Time.deltaTime;
            _stateMachine.Tick();
        }

        //Detection

        /// <summary>
        /// Detects the nearest player within chase range and assigns it as target.
        /// Called from the state machine eval loop, not every frame.
        /// </summary>
        public void DetectPlayer()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _stats.chaseRange, _playerLayer);

            float closestDistance = float.MaxValue;
            Transform closest = null;

            foreach (var hit in hits)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = hit.transform;
                }
            }

            _target = closest;
        }

        public bool IsPlayerInChaseRange() =>
            _target != null && Vector3.Distance(transform.position, _target.position) <= _stats.chaseRange;

        public bool IsPlayerInAttackRange() =>
            _target != null && Vector3.Distance(transform.position, _target.position) <= _stats.attackRange;

        //Combat 

        /// <summary>
        /// Applies damage to the enemy considering defense stats.
        /// </summary>
        /// 
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public virtual void TakeDamagRpc(float rawAmount, DamageType type, bool isCritic, Vector3 hitPos)
        {
            if (!IsServer || _isDead) return;

            float reduction = type == DamageType.PhysicDamage
                ? _stats.physicalDefense
                : _stats.magicDefense;

            float finalDamage = Mathf.Round(rawAmount * (1f - reduction));
            _currentHealth = Mathf.Max(_currentHealth - finalDamage, 0f);

            _damagePopUp.ShowPopUpClientRpc(finalDamage.ToString(), type, isCritic, hitPos);
            _stateMachine.ChangeState(EnemyState.TakingDamage);

            if (_currentHealth <= 0f)
            {
                Die();
                return;
            }

            _enemyAnimation.PlayHitAnimation();
        }

        /// <summary>
        /// Returns a random damage value within the configured range.
        /// </summary>
        protected float RollAttackDamage() =>
            Random.Range(_stats.minAttackDamage, _stats.maxAttackDamage);

        protected virtual void Die()
        {
            Debug.Log("Enemy died");

            _isDead = true;
            if (_agent.isActiveAndEnabled) _agent.isStopped = true; // TODO: delete if when navmesh is implemented
            _stateMachine.ChangeState(EnemyState.Dead);

            _enemyAnimation.PlayDeathAnimation();

            DropLoot();

            // TODO: Grant experience to player when stats system is available.

            StartCoroutine(DespawnRoutine());
        }

        //Loot

        private void DropLoot()
        {
            //TODO
        }

        //private float TotalLootWeight()
        //{
        //TODO
        //}

        //Patrol

        public void SetPatrolPoints(GameObject[] points)
        {
            _patrolPoints = points;
        }
        // Called once when entering Move state
        public void MoveToNextPatrolPoint()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;
            if (_patrolPoints[_currentPatrolIndex] == null) return;

            Vector3 destination = _patrolPoints[_currentPatrolIndex].transform.position;
            Vector2 offset = Random.insideUnitCircle * _wanderRadius;
            destination += new Vector3(offset.x, 0f, offset.y);

            _agent.SetDestination(destination);
        }

        // Called from the eval loop — starts waypoint pause on arrival
        public void CheckPatrolArrival()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;
            if (_isWaypointPausing) return;
            if (_agent.pathPending || _agent.remainingDistance >= 0.5f) return;

            _isWaypointPausing = true;
            StartCoroutine(WaypointPauseRoutine());
        }

        private System.Collections.IEnumerator WaypointPauseRoutine()
        {
            _agent.isStopped = true;

            int lookCount = Random.Range(1, 3);
            for (int i = 0; i < lookCount; i++)
            {
                if (_stateMachine.CurrentState != EnemyState.Move) break;

                Quaternion startRot = transform.rotation;
                Quaternion targetRot = startRot * Quaternion.Euler(0f, Random.Range(-100f, 100f), 0f);
                float elapsed = 0f;
                float rotateDuration = 0.4f;

                while (elapsed < rotateDuration)
                {
                    transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / rotateDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                yield return new WaitForSeconds(Random.Range(0.2f, _waypointPauseDuration));
            }

            _isWaypointPausing = false;

            if (_stateMachine.CurrentState != EnemyState.Move) yield break;

            _agent.isStopped = false;
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
            MoveToNextPatrolPoint();
        }

        //Abstract 

        /// <summary>
        /// Attack logic specific to each enemy type. Implemented by concrete classes.
        /// </summary>
        public abstract void PerformAttackRpc();

        private System.Collections.IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(3f); // It'll be death animation time

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        //Gizmos
        private void OnDrawGizmosSelected()
        {
            if (_stats == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stats.chaseRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _stats.attackRange);
        }

    }
}