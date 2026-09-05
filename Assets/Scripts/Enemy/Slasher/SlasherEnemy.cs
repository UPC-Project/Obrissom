using System.Collections;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Melee enemy. Attack loop: Lunge → Windup → Active frames → Recovery.
    /// Retreats on burst damage. Enrages below HP threshold.
    /// </summary>
    public class SlasherEnemy : EnemyBase
    {
        [Header("Slasher")]
        [SerializeField] private SlasherConfig _slasherConfig;
        [SerializeField] private GameObject _shieldVfx;

        public override bool IsRetreating   => _isRetreating;
        public override bool IsInvulnerable => _isRetreating || Time.time < _invulnerableUntil;

        private SlasherAnimation _slasherAnimation;

        private bool _isComboActive;
        private bool _isRetreating;
        private bool _isEnraged;

        private float _recentDamageAccumulated;
        private float _invulnerableUntil;
        private float _timeSinceLastDamage;
        private float _regenBaseHealth;
        private bool  _regenStarted;
        private bool  _canRegen;

        private Coroutine _damageClearRoutine;

        // Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _slasherAnimation = GetComponent<SlasherAnimation>();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsServer || _isDead || _slasherConfig == null) return;

            CheckEnrageThreshold();

            _timeSinceLastDamage += Time.deltaTime;
            if (_timeSinceLastDamage >= _slasherConfig.regenDelay)
                RegenerateHealth();
        }

        private void RegenerateHealth()
        {
            if (_isRetreating || !_canRegen) return;

            if (!_regenStarted)
            {
                _regenBaseHealth = _currentHealth;
                _regenStarted = true;
            }

            float regenCap = Mathf.Min(_regenBaseHealth * (1f + _slasherConfig.regenCap), _stats.maxHealth);
            if (_currentHealth >= regenCap) return;

            float before = _currentHealth;
            float regenAmount = _stats.maxHealth * _slasherConfig.regenRate * Time.deltaTime;
            _currentHealth = Mathf.Min(_currentHealth + regenAmount, regenCap);
            _enemyUi.UpdateHealthUIRpc(_currentHealth, _stats.maxHealth);

            if ((int)_currentHealth != (int)before)
                Debug.Log($"[Slasher] Regen: {_currentHealth:F1} / {_stats.maxHealth}");
        }

        // Enrage

        private void CheckEnrageThreshold()
        {
            if (_isEnraged) return;

            float healthFraction = _currentHealth / _stats.maxHealth;
            if (healthFraction <= _slasherConfig.enrageHealthThreshold)
            {
                _isEnraged = true;
                Debug.Log("[Slasher] Enraged");
            }
        }

        // Damage

        protected override void OnTakeDamage(float rawAmount)
        {
            Debug.Log($"[Slasher] Hit: {rawAmount} | retreating: {_isRetreating}");
            if (_isRetreating) return;
            _regenStarted = false;
            _canRegen = false;
            TrackBurstDamage(rawAmount);
        }

        private void TrackBurstDamage(float rawAmount)
        {
            _timeSinceLastDamage = 0f;
            _recentDamageAccumulated += rawAmount;

            Debug.Log($"[Slasher] Burst: {_recentDamageAccumulated:F1} / {_slasherConfig.retreatDamageThreshold}");

            if (_damageClearRoutine != null)
                StopCoroutine(_damageClearRoutine);
            _damageClearRoutine = StartCoroutine(ClearBurstDamageAfterWindow());

            if (_recentDamageAccumulated >= _slasherConfig.retreatDamageThreshold)
            {
                _recentDamageAccumulated = 0f;
                Debug.Log("[Slasher] Retreat triggered");
                StartCoroutine(RetreatRoutine());
            }
        }

        private IEnumerator ClearBurstDamageAfterWindow()
        {
            yield return new WaitForSeconds(_slasherConfig.retreatDamageWindow);
            _recentDamageAccumulated = 0f;
        }

        // Attack

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public override void PerformAttackRpc()
        {
            if (_attackCooldownTimer > 0f || _isComboActive || _isRetreating) return;

            _attackCooldownTimer = _stats.attackCooldown;
            StartCoroutine(ComboRoutine());
        }

        private IEnumerator ComboRoutine()
        {
            _isComboActive = true;

            int hits     = _isEnraged ? _slasherConfig.enragedComboHits : _slasherConfig.comboHits;
            float windup = _isEnraged
                ? _slasherConfig.windupDuration * _slasherConfig.enrageWindupMultiplier
                : _slasherConfig.windupDuration;

            if (ShouldLunge())
                yield return StartCoroutine(LungeRoutine());

            for (int i = 0; i < hits; i++)
            {
                if (_isDead || _isRetreating) break;

                yield return new WaitForSeconds(windup);

                if (_isDead || _isRetreating) break;

                PerformSweep();
                _enemyAnimation.PlayAttackAnimation();

                yield return new WaitForSeconds(_slasherConfig.activeFramesDuration);

                bool isLastHit = i == hits - 1;
                if (!isLastHit)
                    yield return new WaitForSeconds(_slasherConfig.delayBetweenHits);
            }

            yield return new WaitForSeconds(_slasherConfig.recoveryDuration);

            _isComboActive = false;
        }

        // Lunge

        private bool ShouldLunge()
        {
            if (_target == null) return false;
            float distanceToTarget = Vector3.Distance(transform.position, _target.position);
            return distanceToTarget > _stats.attackRange * _slasherConfig.lungeTriggerRatio;
        }

        private IEnumerator LungeRoutine()
        {
            if (_target == null) yield break;

            _agent.isStopped = false;
            _agent.speed = _slasherConfig.lungeSpeed;
            _agent.SetDestination(_target.position);

            yield return new WaitForSeconds(_slasherConfig.lungeDuration);

            _agent.isStopped = true;
            _agent.speed = _stats.moveSpeed;
        }

        // Retreat

        private IEnumerator RetreatRoutine()
        {
            _isRetreating = true;
            _isComboActive = false;
            _timeSinceLastDamage = 0f;
            SetShieldVfxRpc(true);

            yield return new WaitForSeconds(_slasherConfig.retreatDelay);

            if (_isDead || _target == null)
            {
                _isRetreating = false;
                SetShieldVfxRpc(false);
                yield break;
            }

            Vector3 retreatDir         = (transform.position - _target.position).normalized;
            Vector3 retreatDestination = transform.position + retreatDir * _slasherConfig.retreatDistance;

            _agent.isStopped = false;
            _agent.speed = _slasherConfig.retreatSpeed;
            _agent.SetDestination(retreatDestination);

            yield return new WaitForSeconds(_slasherConfig.retreatDuration);

            Debug.Log("[Slasher] Retreat done");
            _agent.speed = _stats.moveSpeed;
            _isRetreating = false;
            _canRegen = true;
            _timeSinceLastDamage = _slasherConfig.regenDelay;
            _invulnerableUntil = Time.time + _slasherConfig.postRetreatInvulnerabilityDuration;
            SetShieldVfxRpc(false);

            if (IsPlayerInAttackRange())     _stateMachine.ChangeState(EnemyState.Attack);
            else if (IsPlayerInChaseRange()) _stateMachine.ChangeState(EnemyState.Chase);
            else                             _stateMachine.ChangeState(EnemyState.Idle);
        }

        // Sweep

        private void PerformSweep()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _stats.attackRange, _playerLayer);

            foreach (var hit in hits)
            {
                if (!IsWithinSweepAngle(hit.transform.position)) continue;

                PlayerCombat playerCombat = hit.GetComponentInParent<PlayerCombat>();
                if (playerCombat == null) continue;

                playerCombat.TakeDamage(RollAttackDamage(), _stats.damageType);
            }
        }

        private bool IsWithinSweepAngle(Vector3 targetPosition)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            return angleToTarget <= _slasherConfig.sweepAngle / 2f;
        }

        // Shield VFX

        [Rpc(SendTo.ClientsAndHost)]
        private void SetShieldVfxRpc(bool active)
        {
            if (_shieldVfx != null) _shieldVfx.SetActive(active);
        }

        // Gizmos

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (_slasherConfig == null || _stats == null) return;

            float halfAngle  = _slasherConfig.sweepAngle / 2f;
            Quaternion left  = Quaternion.Euler(0f, -halfAngle, 0f);
            Quaternion right = Quaternion.Euler(0f,  halfAngle, 0f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, left  * transform.forward * _stats.attackRange);
            Gizmos.DrawRay(transform.position, right * transform.forward * _stats.attackRange);
        }
    }
}
