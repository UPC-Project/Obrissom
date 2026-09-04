using System.Collections;
using UnityEngine;

public class Collectable : PickupBase
{
    [SerializeField] protected float _reSpawnTime;
    [SerializeField] private GameObject model;
    [SerializeField] private Collider _trigger;
    [SerializeField, Min(1)] protected int _minQuantity;
    [SerializeField, Min(1)] protected int _maxQuantity;

    public override void OnNetworkSpawn()
    {
        _respawn = true;
        if (_trigger == null) _trigger = gameObject.GetComponent<Collider>();
        _itemID.OnValueChanged += (prev, current) => ResolveItem();
        OnItemChanged += UpdateItem;
        ResolveItem();
    }

    protected override void UpdateItem()
    {
        _quantity.Value = Random.Range(_minQuantity, _maxQuantity + 1);
        model.SetActive(false);
        _trigger.enabled = false;
        StartCoroutine(ReSpawn());
    }

    private IEnumerator ReSpawn()
    {
        yield return new WaitForSecondsRealtime(_reSpawnTime);

        model.SetActive(true);
        _trigger.enabled = true;
    }
}
