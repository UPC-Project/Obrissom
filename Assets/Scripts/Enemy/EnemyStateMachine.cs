using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Controls enemy state transitions and per-state logic.
    /// Transitions are event-driven or polled at a fixed interval — never every frame.
    /// Runs only on the server via EnemyBase.
    /// </summary>
    public class EnemyStateMachine : MonoBehaviour
    {
        private EnemyBase _enemy;
        private NavMeshAgent _agent;

        public EnemyState CurrentState { get; private set; } = EnemyState.None;

        [Header("Timers")]
        [SerializeField] private float _idleDuration = 2f;
        [SerializeField] private float _takingDamageDuration = 0.4f;

        [Header("Polling")]
        [SerializeField] private float _evalInterval = 0.2f;

        private Coroutine _evalLoopCoroutine;

        // Init

        public void Initialize(EnemyBase enemy, NavMeshAgent agent)
        {
            _enemy = enemy;
            _agent = agent;
            ChangeState(EnemyState.Idle);
            _evalLoopCoroutine = StartCoroutine(EvalLoop());
        }

        // Per-frame tick — only smooth visual things (rotation)

        public void Tick()
        {
            if (CurrentState != EnemyState.Chase && CurrentState != EnemyState.Attack) return;
            if (_enemy.Target != null)
                FaceTarget(_enemy.Target.position);
        }

        // Eval loop — range checks and destination updates at fixed interval

        private IEnumerator EvalLoop()
        {
            var wait = new WaitForSeconds(_evalInterval);
            while (true)
            {
                _enemy.DetectPlayer();
                EvaluateTransitions();
                OnStateUpdate();
                yield return wait;
            }
        }

        // Transitions

        private void EvaluateTransitions()
        {
            switch (CurrentState)
            {
                case EnemyState.Idle:
                    if (_enemy.IsPlayerInChaseRange())
                        ChangeState(EnemyState.Chase);
                    break;

                case EnemyState.Move:
                    if (_enemy.IsPlayerInChaseRange())
                        ChangeState(EnemyState.Chase);
                    else
                        _enemy.CheckPatrolArrival();
                    break;

                case EnemyState.Chase:
                    if (_enemy.IsPlayerInAttackRange())
                        ChangeState(EnemyState.Attack);
                    else if (!_enemy.IsPlayerInChaseRange())
                        ChangeState(EnemyState.Move);
                    break;

                case EnemyState.Attack:
                    if (!_enemy.IsPlayerInAttackRange())
                        ChangeState(EnemyState.Chase);
                    break;

                case EnemyState.TakingDamage:
                case EnemyState.Dead:
                    break;
            }
        }

        // State update (interval — not per frame)

        private void OnStateUpdate()
        {
            switch (CurrentState)
            {
                case EnemyState.Chase:
                    _agent.SetDestination(_enemy.Target.position);
                    break;

                case EnemyState.Attack:
                    _enemy.PerformAttackRpc();
                    break;
            }
        }

        // State enter / exit

        public void ChangeState(EnemyState newState)
        {
            if (CurrentState == newState) return;

            OnStateExit(CurrentState);
            CurrentState = newState;
            OnStateEnter(newState);
        }

        private void OnStateEnter(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Idle:
                    _agent.isStopped = true;
                    StartCoroutine(IdleTimer());
                    break;

                case EnemyState.Move:
                    _agent.isStopped = false;
                    _agent.speed = _enemy.Stats.moveSpeed;
                    _enemy.MoveToNextPatrolPoint();
                    break;

                case EnemyState.Chase:
                    _agent.isStopped = false;
                    _agent.speed = _enemy.Stats.moveSpeed * 1.5f;
                    break;

                case EnemyState.Attack:
                    _agent.isStopped = true;
                    break;

                case EnemyState.TakingDamage:
                    _agent.isStopped = true;
                    StartCoroutine(TakingDamageExit());
                    break;

                case EnemyState.Dead:
                    _agent.isStopped = true;
                    if (_evalLoopCoroutine != null)
                        StopCoroutine(_evalLoopCoroutine);
                    break;
            }
        }

        private void OnStateExit(EnemyState state)
        {
            // TODO
        }

        // Timers as coroutines — no per-frame countdown

        private IEnumerator IdleTimer()
        {
            yield return new WaitForSeconds(_idleDuration);
            if (CurrentState == EnemyState.Idle)
                ChangeState(EnemyState.Move);
        }

        private IEnumerator TakingDamageExit()
        {
            yield return new WaitForSeconds(_takingDamageDuration);
            if (CurrentState != EnemyState.TakingDamage) yield break;

            if (_enemy.IsPlayerInAttackRange())
                ChangeState(EnemyState.Attack);
            else if (_enemy.IsPlayerInChaseRange())
                ChangeState(EnemyState.Chase);
            else
                ChangeState(EnemyState.Idle);
        }

        // Helpers

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - _enemy.transform.position;
            direction.y = 0f;
            if (direction == Vector3.zero) return;
            _enemy.transform.rotation = Quaternion.Slerp(
                _enemy.transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 10f
            );
        }
    }
}
