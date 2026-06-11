using UnityEngine;

public class TestEnemy : MonoBehaviour
{
    public void TakeDamage(float damageAmount, DamageType damageType, bool critic, Vector3 hitPos)
    {
        if (damageType == DamageType.MagicDamage)
        {
            MagicDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount.ToString(), critic);
        }
        else
        {
            PhyiscDamagePopUpPool.Instance.CreatePopUp(hitPos, damageAmount.ToString(), critic);
        }
    }
}
