using UnityEngine;

public class TestXPGetter : MonoBehaviour
{
    public int xpAmount = 50;
    private void OnTriggerEnter(Collider collision)
    {
        Obrissom.Player.PlayerXP playerXP = collision.gameObject.GetComponent<Obrissom.Player.PlayerXP>();
        if (playerXP != null && playerXP.IsOwner)
        {
            playerXP.GainXP(xpAmount);
        }
    }
}
