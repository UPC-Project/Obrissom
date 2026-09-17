using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Collectable : PickupBase
{
    [SerializeField] protected float _reSpawnTime;
    [SerializeField] private GameObject model;
    [SerializeField] private Collider _trigger;
    [SerializeField, Min(1)] protected int _minQuantity;
    [SerializeField, Min(1)] protected int _maxQuantity;

    // Syncs visibility and interactability state to late-joining clients
    private NetworkVariable<bool> _isAvailable = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        _respawn = true;
        if (_trigger == null) _trigger = gameObject.GetComponent<Collider>();
        
        _isAvailable.OnValueChanged += OnAvailabilityChanged;
        OnAvailabilityChanged(false, _isAvailable.Value);
        
        base.OnNetworkSpawn();
    }

    private void OnAvailabilityChanged(bool previousValue, bool newValue)
    {
        model.SetActive(newValue);
        _trigger.enabled = newValue;
    }

    protected override bool CanBePickedUp()
    {
        return _isAvailable.Value;
    }

    protected override void OnPickupServer()
    {
        _quantity.Value = Random.Range(_minQuantity, _maxQuantity + 1);
        _isAvailable.Value = false;
        StartCoroutine(ReSpawnServer());
    }

    private IEnumerator ReSpawnServer()
    {
        yield return new WaitForSecondsRealtime(_reSpawnTime);
        _isAvailable.Value = true;
    }
}
