using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;
using Obrissom.Player.Inventory;

/// <summary>
/// This script manages the physical item dropped in the 3D world.
/// It handles visuals (floating/rotating), floating labels, and the pickup logic.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class WorldItem : NetworkBehaviour
{
    [Header("Pickup Settings")]
    public bool autoPickup = false;

    [Header("Visuals")]
    [SerializeField] private float _bobSpeed = 1.5f;     
    [SerializeField] private float _bobHeight = 0.15f;   
    [SerializeField] private float _rotationSpeed = 90f; 

    [Header("Label")]
    [SerializeField] private Transform _labelTransform; 
    [SerializeField] private TextMeshProUGUI _labelText; 

    [Header("Detection")]
    [SerializeField] private float _pickupRadius = 2f;   
    [SerializeField] private LayerMask _playerLayer;      

    // --- NETWORK VARIABLES ---
    // These sync automatically from Server to all Clients.
    private NetworkVariable<int> _quantity = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _itemID = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- LOCAL VARIABLES ---
    private Item _item;              // The resolved Item data from the database
    private Vector3 _startPosition;  // Original position for the floating animation
    private bool _playerInRange;     // Is a player near enough to pick this up?
    private Camera _camera;          // Cached reference to the main camera

    public override void OnNetworkSpawn()
    {
        _startPosition = transform.position;
        GetComponent<Collider>().isTrigger = true; // Ensure the collider is a trigger
        _camera = Camera.main;

        if (_labelText == null)
            _labelText = GetComponentInChildren<TextMeshProUGUI>();

        // Subscribe to value changes: if the ID or Quantity changes on the server, update the UI
        _itemID.OnValueChanged += (prev, current) => ResolveItem();
        _quantity.OnValueChanged += (prev, current) => UpdateLabel();

        // Initial attempt to resolve the item data
        ResolveItem();
    }

    /// <summary>
    /// Set by the Server right after spawning the object.
    /// </summary>
    public void InitializeItem(Item item, int quantity)
    {
        if (!IsServer) return;
        _itemID.Value = item.itemID;
        _quantity.Value = quantity;
    }

    /// <summary>
    /// Asks the ItemDatabase for the full Item data using the synced ID.
    /// </summary>
    private void ResolveItem()
    {
        if (_itemID.Value == -1) return;

        if (Obrissom.Database.ItemDatabase.Instance != null)
        {
            _item = Obrissom.Database.ItemDatabase.Instance.GetItemByID(_itemID.Value);
            UpdateLabel();
        }
    }

    private void Update()
    {
        if (!IsSpawned) return;

        AnimateFloat();

        // Make the floating label always face the camera (Billboarding)
        if (_camera == null) _camera = Camera.main;
        if (_labelTransform != null && _camera != null)
            _labelTransform.rotation = _camera.transform.rotation;

        DetectPlayer();

        // If player is close and presses 'F', request pickup to the server
        if (_playerInRange && !autoPickup && Keyboard.current[Key.F].wasPressedThisFrame)
            RequestPickupServerRpc();
    }

    /// <summary>
    /// Simple floating and rotating animation.
    /// </summary>
    private void AnimateFloat()
    {
        float newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Updates the 3D text showing the Item Name and Quantity.
    /// </summary>
    private void UpdateLabel()
    {
        if (_labelText == null) return;

        string text = _item != null ? _item.itemName : "Loading...";
        if (_quantity.Value > 1)
            text += " x" + _quantity.Value;

        _labelText.text = text;
    }

    /// <summary>
    /// Uses physics to check if any player is inside the pickup radius.
    /// </summary>
    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRadius, _playerLayer);
        _playerInRange = hits.Length > 0;
    }

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
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}