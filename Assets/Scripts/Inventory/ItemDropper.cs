using Obrissom.Player.Inventory;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This service handles spawning items in the 3D world.
/// It is used when a player drops an item or an enemy dies.
/// </summary>
public class ItemDropper : NetworkBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject _worldItemPrefab; // The prefab with WorldItem script
    [SerializeField] private float _dropForce = 3f;        // Forward power of the drop
    [SerializeField] private float _dropUpwardForce = 2f; // Upward power of the drop

    public override void OnNetworkSpawn()
    {
        // Only the local player (Owner) connects their dropper to the Global UI
        if (!IsOwner) return;

        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.RegisterPlayerItemDropper(this);
        }
    }

    /// <summary>
    /// Starts the process of dropping an item. 
    /// If called on a client, it sends a request to the server.
    /// </summary>
    public void DropItem(Item item, int quantity = 1)
    {
        if (item == null || _worldItemPrefab == null) return;

        if (IsServer)
        {
            SpawnWorldItem(item, quantity);
        }
        else
        {
            // Send the Item ID to the server to identify the object
            DropItemServerRpc(item.itemID, quantity);
        }
    }

    /// <summary>
    /// The Server receives the request and finds the item in the database.
    /// </summary>
    [ServerRpc]
    private void DropItemServerRpc(int itemID, int quantity)
    {
        Item item = Obrissom.Database.ItemDatabase.Instance.GetItemByID(itemID);

        if (item != null)
        {
            SpawnWorldItem(item, quantity);
        }
    }

    /// <summary>
    /// Creates the physical object in the world and gives it velocity.
    /// This MUST run on the server to be seen by everyone.
    /// </summary>
    private void SpawnWorldItem(Item item, int quantity)
    {
        // Calculate spawn position in front of the character
        Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
        GameObject dropped = Instantiate(_worldItemPrefab, dropPosition, Random.rotation);

        // Network Spawn: syncs the new object with all connected players
        NetworkObject networkObject = dropped.GetComponent<NetworkObject>();
        networkObject.Spawn();

        // Initialize the item data (ID and Quantity) on the world object
        WorldItem worldItem = dropped.GetComponent<WorldItem>();
        worldItem.InitializeItem(item, quantity);

        // Apply physical force
        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = transform.forward * _dropForce + Vector3.up * _dropUpwardForce;
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// This is called by the Server to inform a specific Client that 
    /// an item was added to their backpack.
    /// </summary>
    [ClientRpc]
    public void ReceiveItemClientRpc(int itemID, int quantity, ClientRpcParams rpcParams = default)
    {
        Inventory myInventory = GetComponent<Inventory>();
        if (myInventory == null) return;

        // Find the item in the local database
        Item item = Obrissom.Database.ItemDatabase.Instance.GetItemByID(itemID);

        if (item != null)
        {
            // Add the item to the local data to trigger UI refresh
            myInventory.AddItem(item, quantity);
        }
    }

}