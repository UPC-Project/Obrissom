using System.Collections;
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

    [Header("Herd")]
    [Tooltip("Optional. If empty, one is created here.")]
    [SerializeField] private EnemyHerd _herd;
    [SerializeField] private bool _spawnAsHerd = true;
    [Tooltip("Not used with patrol points.")]
    [SerializeField] private float _herdRoamRadius = 15f;
    [Tooltip("Random pause at each destination, in seconds.")]
    [SerializeField] private Vector2 _herdPause = new Vector2(2f, 5f);

    [Header("Respawn")]
    [Tooltip("Seconds after an enemy dies until a new one appears here. 0 = never.")]
    [SerializeField] private float _respawnDelay = 30f;

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
        enemy.Died += OnEnemyDied;

        if (enemy is IHerdMember member)
        {
            EnemyHerd herd = GetOrCreateHerd();
            if (herd != null) herd.AddMember(member);
        }
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        enemy.Died -= OnEnemyDied;
        if (_respawnDelay > 0f && IsSpawned) StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);
        if (IsSpawned && _enemyPrefab != null) SpawnEnemy();
    }

    private EnemyHerd GetOrCreateHerd()
    {
        if (_herd == null && _spawnAsHerd)
        {
            _herd = gameObject.AddComponent<EnemyHerd>();
            _herd.Configure(_herdRoamRadius, _herdPause, _patrolPoints, destroyWhenEmpty: false);
        }
        return _herd;
    }
}
