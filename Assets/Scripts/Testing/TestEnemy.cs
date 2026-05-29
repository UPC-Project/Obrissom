using UnityEngine;
using Obrissom.UI;

public class TestEnemy : MonoBehaviour
{
    public void TakeDamage(float damageAmount, DamageType damageType, bool critic, Vector3 hitPos)
    {
        DamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount.ToString(), damageType, critic);
    }
}