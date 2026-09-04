using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class PickupBase : NetworkBehaviour
{
    [Header("Pickup Settings")]
    public bool autoPickup = false;
    [SerializeField] protected bool _respawn = false;
    [SerializeField] protected Item _item;
    public event Action OnItemChanged;

    // NETWORK VARIABLES 
    protected NetworkVariable<int> _quantity = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    protected NetworkVariable<int> _itemID = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        _itemID.OnValueChanged += (prev, current) => ResolveItem();
        OnItemChanged += UpdateItem;
        ResolveItem();
    }

    public override void OnNetworkDespawn()
    {
        OnItemChanged -= UpdateItem;
    }

    /// <summary>
    /// Asks the ItemDatabase for the full Item data using the synced ID.
    /// </summary>
    protected virtual void ResolveItem()
    {
        _itemID.Value = _item.itemID;
    }


    /// <summary>
    /// Called by PlayerInteraction when the player presses the interact button.
    /// autoPickup = false
    /// </summary>
    public void Interact()
    {
        if (!autoPickup)
        {
            OnItemChanged.Invoke();
            RequestPickupServerRpc();
        }
    }

    /// autoPickup = true
    private void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned || !autoPickup) return;

        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            OnItemChanged.Invoke();
            RequestPickupServerRpc();
        }
    }
    protected virtual void UpdateItem()  {}

    /// <summary>
    /// CLIENT -> SERVER: "I want to pick this item up".
    /// </summary>
    [Rpc(SendTo.Server)]
    private void RequestPickupServerRpc(RpcParams rpcParams = default)
    {
        if (_item == null) return;

        // 1. Get the ID of the client who pressed 'F'
        ulong clientId = rpcParams.Receive.SenderClientId;

        // 2. Find that player's object in the network
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            ItemDropper playerDropper = client.PlayerObject.GetComponent<ItemDropper>();

            if (playerDropper != null)
            {
                // 3. Prepare message only for this specific client
                ClientRpcParams targetParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                };

                // 4. Send the item data to that player's inventory
                playerDropper.ReceiveItemClientRpc(_itemID.Value, _quantity.Value, targetParams);

                // 5. Despawn the world object (it will disappear for everyone)
                if (!_respawn) GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
