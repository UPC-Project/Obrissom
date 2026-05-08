using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    public void TakeDamage(float damageAmount, DamageType damageType)
    {
        Debug.Log($"Enemy took {damageAmount} damage of type {damageType}");
    }
}