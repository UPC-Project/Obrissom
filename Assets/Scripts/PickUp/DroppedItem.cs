using UnityEngine;
using TMPro;

/// <summary>
/// This script manages the physical item dropped in the 3D world.
/// It handles visuals (floating/rotating), floating labels, and the pickup logic.
/// Represents items dropped by the player through inventory, or enemy drops.
/// </summary>

public class DroppedItem : PickupBase
{
    [Header("Dropped Item Settigns")]
    [Header("Visuals")]
    [SerializeField] private float _bobSpeed = 1.5f;
    [SerializeField] private float _bobHeight = 0.15f;
    [SerializeField] private float _rotationSpeed = 90f;

    [Header("Label")]
    [SerializeField] private Transform _labelTransform;
    [SerializeField] private TextMeshProUGUI _labelText;

    // LOCAL VARIABLES
    private Vector3 _startPosition;
    private Camera _camera;

    public override void OnNetworkSpawn()
    {
        _startPosition = transform.position;
        GetComponent<Collider>().isTrigger = true;
        _camera = Camera.main;

        if (_labelText == null)
            _labelText = GetComponentInChildren<TextMeshProUGUI>();

        _itemID.OnValueChanged += (prev, current) => ResolveItem();
        _quantity.OnValueChanged += (prev, current) => UpdateLabel();

        ResolveItem();
    }

    /// <summary>
    /// Asks the ItemDatabase for the full Item data using the synced ID.
    /// </summary>
    protected override void ResolveItem()
    {
        if (_itemID.Value == -1) return;

        if (Obrissom.Database.ItemDatabase.Instance != null)
        {
            _item = Obrissom.Database.ItemDatabase.Instance.GetItemByID(_itemID.Value);
            UpdateLabel();
        }
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

    private void Update()
    {
        if (!IsSpawned) return;

        AnimateFloat();

        // Make the floating label always face the camera (Billboarding)
        if (_camera == null) _camera = Camera.main;
        if (_labelTransform != null && _camera != null)
            _labelTransform.rotation = _camera.transform.rotation;
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

}
