using UnityEngine;
using Obrissom.Enemy;
using Obrissom.Combat;

public class Enemy : EnemyBase
{
    public override void PerformAttack()
    {
        Debug.Log("TestEnemy atacando al jugador.");
    }


    [ContextMenu("Test Take Damage")]
    public void TestTakeDamage()
    {
        if (IsServer)
        {
            TakeDamage(20f, DamageType.PhysicDamage);
            Debug.Log($"Prueba de Daño. Vida restante: {_currentHealth}");
        }
    }
}