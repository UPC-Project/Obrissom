using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Collectable : PickupBase
{
    public enum HideMode
    {
        DisableModel,
        HideMaterial
    }

    [SerializeField] protected float _reSpawnTime;
    [SerializeField] private HideMode _hideMode = HideMode.DisableModel;
    [Tooltip("Only for DisableModel")]
    [SerializeField] private GameObject model;
    [Tooltip("Only for HideMaterial")]
    [SerializeField] private Renderer _renderer;
    [Tooltip("Only for HideMaterial")]
    [SerializeField] private Material _targetMaterial;
    [Tooltip("Only for HideMaterial")]
    [SerializeField] private Material _hiddenMaterial;
    [SerializeField] private Collider _trigger;
    [SerializeField, Min(1)] protected int _minQuantity;
    [SerializeField, Min(1)] protected int _maxQuantity;

    private int _materialIndex = -1;

    // Syncs visibility and interactability state to late-joining clients
    private NetworkVariable<bool> _isAvailable = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // False while it waits to respawn after being picked
    public bool IsAvailable => !IsSpawned || _isAvailable.Value;

    private void Awake()
    {
        if (_hideMode == HideMode.HideMaterial)
            _materialIndex = Array.IndexOf(_renderer.sharedMaterials, _targetMaterial);
    }

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
        if (_hideMode == HideMode.DisableModel)
        {
            model.SetActive(newValue);
        }
        else
        {
            Material[] materials = _renderer.sharedMaterials;
            materials[_materialIndex] = newValue ? _targetMaterial : _hiddenMaterial;
            _renderer.sharedMaterials = materials;
        }

        _trigger.enabled = newValue;
        if (!newValue) SetInteractPromptActive(false);
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
