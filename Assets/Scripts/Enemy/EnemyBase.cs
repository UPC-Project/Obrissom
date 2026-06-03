using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Obrissom.Combat;

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
            // Only the server runs enemy logic
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

        /// <summary>
        /// Applies damage to the enemy considering defense stats.
        /// </summary>
        public virtual void TakeDamage(float rawAmount, DamageType type)
        {
            if (!IsServer || _isDead) return;

            float reduction = type == DamageType.PhysicDamage
                ? _stats.physicalDefense
                : _stats.magicDefense;

            float finalDamage = rawAmount * (1f - reduction);
            _currentHealth -= finalDamage;
            _currentHealth = Mathf.Max(_currentHealth, 0f);

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
            //_isDead = true;
            //_agent.isStopped = true;

            //_enemyAnimation.PlayDeathAnimation();

            //DropLoot();

            // TODO: Grant experience to player when stats system is available.

            Destroy(gameObject, 3f);
        }

        //Loot

        private void DropLoot()
        {
            if (_stats.lootTable == null || _stats.lootTable.Length == 0) return;

            foreach (var entry in _stats.lootTable)
            {
                if (entry.item == null) continue;

                float roll = Random.Range(0f, TotalLootWeight());
                float cumulative = 0f;

                foreach (var e in _stats.lootTable)
                {
                    cumulative += e.weight;
                    if (roll <= cumulative)
                    {
                        _itemDropper.DropItem(e.item, e.quantity);
                        break;
                    }
                }
            }
        }

        private float TotalLootWeight()
        {
            float total = 0f;
            foreach (var entry in _stats.lootTable)
                total += entry.weight;
            return total;
        }

        //Patrol

        protected void PatrolToNextPoint()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;

            _agent.SetDestination(_patrolPoints[_currentPatrolIndex].position);

            if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
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
    }
}