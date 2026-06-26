using Obrissom.Enemy;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private GameObject _enemyPrefab;

    [Header("Patrol")]
    [SerializeField] private GameObject[] _patrolPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        GameObject instantiatedEnemy;
        try
        {
            instantiatedEnemy = Instantiate(_enemyPrefab, transform.position, transform.rotation);
        }
        catch
        {
            Debug.LogWarning("[EnemySpawner] _enemyPrefab not assigned in inspector, skipping spawn.");
            return;
        }

        NetworkObject netObj = instantiatedEnemy.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        // Assign patrol points after Spawn so OnNetworkSpawn of EnemyBase already ran
        EnemyBase enemy = instantiatedEnemy.GetComponent<EnemyBase>();
        enemy.SetPatrolPoints(_patrolPoints);
    }
}
