using UnityEngine;
using Obrissom.Enemy;

public class EnemyPatrolSetup : MonoBehaviour
{
    [SerializeField] private EnemyBase _enemy;
    [SerializeField] private GameObject[] _patrolPoints;

    private void Awake()
    {
        _enemy.SetPatrolPoints(_patrolPoints);
    }
}