using UnityEngine;

public class TestXPGetter : MonoBehaviour
{
    public int xpAmount = 50;
    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log($"Collided with {collision.gameObject.name}");
        PlayerXP playerXP = collision.gameObject.GetComponent<PlayerXP>();
        if (playerXP != null)
        {
            playerXP.GainXP(xpAmount);
            Debug.Log($"Gained {xpAmount} XP");
        }
    }
}
