using Obrissom.Enemy;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform _spawnPoint;


    [Header("Patrol")]
    [SerializeField] private GameObject[] _patrolPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        GameObject instantiatedEnemy = Instantiate(_enemyPrefab, _spawnPoint.position, _spawnPoint.rotation);

        EnemyBase enemy = instantiatedEnemy.GetComponent<EnemyBase>();
        enemy.SetPatrolPoints(_patrolPoints);

        NetworkObject netObj = instantiatedEnemy.GetComponent<NetworkObject>();
        netObj.Spawn(true);
    }
}