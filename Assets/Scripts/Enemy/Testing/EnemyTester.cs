using UnityEngine;
using UnityEngine.InputSystem;
using Obrissom.Enemy;

public class EnemyTester : MonoBehaviour
{
    [SerializeField] private float _testDamage = 25f;
    [SerializeField] private DamageType _testDamageType = DamageType.PhysicDamage;

    private void Update()
    {
        var kb = Keyboard.current;

        // T: apply physical damage
        if (kb.tKey.wasPressedThisFrame)
        {
            EnemyTest activeEnemy = GetActiveEnemy();
            activeEnemy.TakeDamageServerRpc(_testDamage, _testDamageType);
        }
        // K: kill enemy
        if (kb.kKey.wasPressedThisFrame)
        {
            EnemyTest activeEnemy = GetActiveEnemy();
            activeEnemy.TakeDamageServerRpc(99999f, DamageType.PhysicDamage);
        }

        // P: PerformAttack
        if (kb.pKey.wasPressedThisFrame)
        {
            EnemyTest activeEnemy = GetActiveEnemy();
            activeEnemy.ForceAttackServerRpc();
        }

    }

    private EnemyTest GetActiveEnemy()
    {
        EnemyTest enemyInScene = FindAnyObjectByType<EnemyTest>();
        return enemyInScene;
    }
}
