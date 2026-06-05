using System.Collections.Generic;
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
    public abstract class EnemyBase : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] protected EnemyStats _stats;

        [Header("Detection")]
        [SerializeField] protected LayerMask _playerLayer;

        [Header("Patrol")]
        [SerializeField] protected Transform[] _patrolPoints;

        // Components
        protected NavMeshAgent _agent;
        protected EnemyAnimation _enemyAnimation;
        protected ItemDropper _itemDropper;

        // Runtime state
        protected float _currentHealth;
        protected float _attackCooldownTimer;
        protected bool _isDead;
        protected int _currentPatrolIndex;
        protected Transform _target;

        // Lifecycle

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _enemyAnimation = GetComponent<EnemyAnimation>();
            _itemDropper = GetComponent<ItemDropper>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            _currentHealth = _stats.maxHealth;
            _agent.speed = _stats.moveSpeed;
        }

        protected virtual void Update()
        {
            if (!IsServer || _isDead) return;

            _attackCooldownTimer -= Time.deltaTime;
        }

        //Detection

        /// <summary>
        /// Detects the nearest player within chase range and assigns it as target.
        /// </summary>
        protected void DetectPlayer()
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

        protected bool IsPlayerInChaseRange() =>
            _target != null && Vector3.Distance(transform.position, _target.position) <= _stats.chaseRange;

        protected bool IsPlayerInAttackRange() =>
            _target != null && Vector3.Distance(transform.position, _target.position) <= _stats.attackRange;

        //Combat 


        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void TakeDamageServerRpc(float rawAmount, DamageType type)
        {
            TakeDamage(rawAmount, type);
        }

        /// <summary>
        /// Applies damage to the enemy considering defense stats.
        /// </summary>
        /// 
        public virtual void TakeDamage(float rawAmount, DamageType type)
        {

            if (!IsServer || _isDead) return;


            float reduction = type == DamageType.PhysicDamage
                ? _stats.physicalDefense
                : _stats.magicDefense;

            float finalDamage = rawAmount * (1f - reduction);
            _currentHealth -= finalDamage;
            _currentHealth = Mathf.Max(_currentHealth, 0f);

            Debug.Log($"[Enemy] Took {finalDamage} damage. Remaining health: {_currentHealth}/{_stats.maxHealth}");

            _enemyAnimation.PlayHitAnimation();

            if (_currentHealth <= 0f)
                Die();
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
            _agent.isStopped = true;

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

        protected void PatrolToNextPoint()
        {
            //TODO
        }

        //Abstract 

        /// <summary>
        /// Attack logic specific to each enemy type. Implemented by concrete classes.
        /// </summary>
        public abstract void PerformAttack();



        //Gizmos
        private void OnDrawGizmosSelected()
        {
            if (_stats == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stats.chaseRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _stats.attackRange);
        }

        private System.Collections.IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(3f);

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}