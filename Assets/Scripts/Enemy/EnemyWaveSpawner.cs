using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    public class EnemyWaveSpawner : NetworkBehaviour
    {
        [Header("Enemy")]
        [SerializeField] private GameObject _enemyPrefab;

        [Header("Patrol")]
        [SerializeField] private GameObject[] _patrolPoints;

        [Header("Wave Config")]
        [SerializeField] private int _minEnemiesPerWave = 3;
        [SerializeField] private int _maxEnemiesPerWave = 5;
        [SerializeField] private float _timeBetweenWaves = 5f;

        [Header("Spawn Area")]
        [SerializeField] private float _spawnRadius = 8f;
        [SerializeField] private float _navMeshSampleDistance = 3f;

        private int _aliveCount;
        private int _currentWave;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            StartCoroutine(SpawnWave());
        }

        private IEnumerator SpawnWave()
        {
            _currentWave++;
            int count = UnityEngine.Random.Range(_minEnemiesPerWave, _maxEnemiesPerWave + 1);
            _aliveCount = 0;

            Debug.Log($"[WaveSpawner] Wave {_currentWave} — spawning {count} enemies");

            for (int i = 0; i < count; i++)
            {
                if (!TryGetNavMeshPosition(out Vector3 spawnPos)) continue;

                GameObject obj = Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);
                EnemyBase enemy = obj.GetComponent<EnemyBase>();
                enemy.SetPatrolPoints(_patrolPoints);
                enemy.OnDeath += OnEnemyDied;

                obj.GetComponent<NetworkObject>().Spawn(true);
                _aliveCount++;

                yield return new WaitForSeconds(0.2f);
            }
        }

        private void OnEnemyDied()
        {
            _aliveCount--;
            Debug.Log($"[WaveSpawner] Enemy died — {_aliveCount} remaining");

            if (_aliveCount <= 0)
                StartCoroutine(NextWaveRoutine());
        }

        private IEnumerator NextWaveRoutine()
        {
            Debug.Log($"[WaveSpawner] Wave {_currentWave} cleared — next wave in {_timeBetweenWaves}s");
            yield return new WaitForSeconds(_timeBetweenWaves);
            StartCoroutine(SpawnWave());
        }

        private bool TryGetNavMeshPosition(out Vector3 result)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _spawnRadius;
                Vector3 candidate = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = transform.position;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }
    }
}
