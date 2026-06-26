using UnityEngine;
using UnityEngine.InputSystem;
using Obrissom.Enemy;
using Unity.Netcode;

public class EnemyTester : MonoBehaviour
{
    [SerializeField] private float _testDamage = 25f;
    [SerializeField] private DamageType _testDamageType = DamageType.PhysicDamage;

    private void Update()
    {
        var kb = Keyboard.current;
        NetworkObjectReference netObj = default;

        // T: apply physical damage
        if (kb.tKey.wasPressedThisFrame)
        {
            EnemyTest activeEnemy = GetActiveEnemy();
            activeEnemy.TakeDamagRpc(_testDamage, _testDamageType, false, activeEnemy.transform.position, netObj);
        }
        // K: kill enemy
        if (kb.kKey.wasPressedThisFrame)
        {
            EnemyTest activeEnemy = GetActiveEnemy();
            activeEnemy.TakeDamagRpc(99999f, DamageType.PhysicDamage, false, activeEnemy.transform.position, netObj);
        }

        // P: PerformAttack
        if (kb.pKey.wasPressedThisFrame)
        {
            EnemyTest activeEnemy = GetActiveEnemy();
            activeEnemy.PerformAttackRpc();
        }
    }

    private EnemyTest GetActiveEnemy()
    {
        EnemyTest enemyInScene = FindAnyObjectByType<EnemyTest>();
        return enemyInScene;
    }
}
