using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Controls enemy state transitions and per-state logic.
    /// Runs only on the server via EnemyBase.Update().
    /// Attach to the same GameObject as EnemyBase.
    /// </summary>
    public class EnemyStateMachine : MonoBehaviour
    {
        private EnemyBase _enemy;
        private NavMeshAgent _agent;

        public EnemyState CurrentState { get; private set; }

        [Header("Timers")]
        [SerializeField] private float _idleDuration = 2f;
        [SerializeField] private float _takingDamageDuration = 0.4f;

        private float _idleTimer;
        private float _takingDamageTimer;

        //Init

        public void Initialize(EnemyBase enemy, NavMeshAgent agent)
        {

            _enemy = enemy;
            _agent = agent;
            ChangeState(EnemyState.Idle);
        }

        //Core

        public void Tick()
        {
            EvaluateTransitions();
            OnStateUpdate();
        }

        //Transitions

        private void EvaluateTransitions()
        {
            switch (CurrentState)
            {
                case EnemyState.Idle:
                    _idleTimer -= Time.deltaTime;
                    if (_enemy.IsPlayerInChaseRange())
                        ChangeState(EnemyState.Chase);
                    else if (_idleTimer <= 0f)
                        ChangeState(EnemyState.Move);
                    break;

                case EnemyState.Move:
                    if (_enemy.IsPlayerInChaseRange())
                        ChangeState(EnemyState.Chase);
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
                    _takingDamageTimer -= Time.deltaTime;
                    if (_takingDamageTimer > 0f) break;

                    if (_enemy.IsPlayerInAttackRange())
                        ChangeState(EnemyState.Attack);
                    else if (_enemy.IsPlayerInChaseRange())
                        ChangeState(EnemyState.Chase);
                    else
                        ChangeState(EnemyState.Idle);
                    break;

                case EnemyState.Dead:
                    break;
            }
        }

        //State update 

        private void OnStateUpdate()
        {
            switch (CurrentState)
            {
                case EnemyState.Idle:
                    break;

                case EnemyState.Move:
                    _enemy.PatrolToNextPoint();
                    break;

                case EnemyState.Chase:
                    _agent.SetDestination(_enemy.Target.position);
                    FaceTarget(_enemy.Target.position);
                    break;

                case EnemyState.Attack:
                    FaceTarget(_enemy.Target.position);
                    _enemy.PerformAttackRpc();
                    break;

                case EnemyState.TakingDamage:
                    break;

                case EnemyState.Dead:
                    break;
            }
        }

        // State enter-exit

        public void ChangeState(EnemyState newState)
        {
            Debug.Log($"[StateMachine] {CurrentState} -> {newState}");

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
                    _idleTimer = _idleDuration;
                    break;

                case EnemyState.Move:
                    _agent.isStopped = false;
                    _agent.speed = _enemy.Stats.moveSpeed;
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
                    _takingDamageTimer = _takingDamageDuration;
                    break;

                case EnemyState.Dead:
                    _agent.isStopped = true;
                    break;
            }
        }

        private void OnStateExit(EnemyState state)
        {
            // TODO
        }


        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - _enemy.transform.position);
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