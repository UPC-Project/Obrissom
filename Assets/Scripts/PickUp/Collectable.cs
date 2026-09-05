using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Obrissom.Player;

public class Collectable : PickupBase
{
    [SerializeField] protected float _reSpawnTime;
    [SerializeField] private GameObject model;
    [SerializeField] private Collider _trigger;
    [SerializeField, Min(1)] protected int _minQuantity;
    [SerializeField, Min(1)] protected int _maxQuantity;

    private NetworkVariable<bool> _isActive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        _respawn = true;
        if (_trigger == null) _trigger = gameObject.GetComponent<Collider>();
        
        _isActive.OnValueChanged += OnActiveStateChanged;
        _itemID.OnValueChanged += (prev, current) => ResolveItem();
        
        ResolveItem();
        
        OnActiveStateChanged(true, _isActive.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isActive.OnValueChanged -= OnActiveStateChanged;
    }

    private void OnActiveStateChanged(bool previous, bool current)
    {
        if (model != null) model.SetActive(current);
        if (_trigger != null) _trigger.enabled = current;

        if (!current && PlayerInteraction.LocalInstance != null)
        {
            PlayerInteraction.LocalInstance.RemoveItem(this);
        }
    }

    protected override bool CanBePickedUp()
    {
        return _isActive.Value;
    }

    // This is now called ONLY on the server after a successful pickup
    protected override void OnPickedUpServer()
    {
        // Randomize quantity for the next time it's picked up
        _quantity.Value = Random.Range(_minQuantity, _maxQuantity + 1);
        
        // Hide the item for all clients
        _isActive.Value = false;
        
        // Start the respawn timer on the server
        StartCoroutine(ReSpawn());
    }

    private IEnumerator ReSpawn()
    {
        yield return new WaitForSecondsRealtime(_reSpawnTime);
        // Show the item for all clients
        _isActive.Value = true;
    }
}
