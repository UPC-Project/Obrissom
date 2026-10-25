using Unity.Netcode;

public class PlayerHealPopUp : NetworkBehaviour
{
    [Rpc(SendTo.Everyone)]
    public void ShowPopUpClientRpc(string healAmount, bool isCritic)
    {
        HealPopUpPool.Instance.CreatePopUp(transform.position, healAmount, isCritic);
    }
}
