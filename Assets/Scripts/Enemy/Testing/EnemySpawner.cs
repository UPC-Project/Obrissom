using Obrissom.Enemy;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int _enemyCount = 1;
    [SerializeField] private float _spawnRadius = 2f;

    [Header("Patrol")]
    [SerializeField] private GameObject[] _patrolPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (_enemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] _enemyPrefab not assigned in inspector, skipping spawn.");
            return;
        }

        for (int i = 0; i < _enemyCount; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector2 offset2D = Random.insideUnitCircle * _spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);

        GameObject instantiatedEnemy = Instantiate(_enemyPrefab, spawnPosition, transform.rotation);

        NetworkObject netObj = instantiatedEnemy.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        // Assign patrol points after Spawn so OnNetworkSpawn of EnemyBase already ran
        EnemyBase enemy = instantiatedEnemy.GetComponent<EnemyBase>();
        enemy.SetPatrolPoints(_patrolPoints);
    }
}
