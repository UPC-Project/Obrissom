using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;


/// <summary>
/// Component of the 3D prefab that represents a physical item in the world.
/// Attach to the item prefab. Handles auto and manual pickup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private Item _item;
    [SerializeField] private int _quantity = 1;

    public Item item
    {
        get => _item;
        set { _item = value; UpdateLabel(); }
    }

    public int quantity
    {
        get => _quantity;
        set { _quantity = value; UpdateLabel(); }
    }

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

    private Vector3 _startPosition;
    private bool _playerInRange = false;
    private Inventory _playerInventory;


    private void Start()
    {
        _startPosition = transform.position;
        GetComponent<Collider>().isTrigger = true;

        if (_labelText == null)
            _labelText = GetComponentInChildren<TextMeshProUGUI>();

        UpdateLabel();
    }

    private void Update()
    {
        AnimateFloat();

        if (_labelTransform != null)
            _labelTransform.rotation = Camera.main.transform.rotation;

        DetectPlayer();

        if (_playerInRange && !autoPickup && Keyboard.current[Key.F].wasPressedThisFrame)
            TryPickup();

    }

    //Visuals 

    private void AnimateFloat()
    {
        float newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }

    private void UpdateLabel()
    {
        if (_labelText == null) return;

        string text = _item != null ? _item.itemName : "Unknown";
        if (_quantity > 1)
            text += $" x{_quantity}";

        _labelText.text = text;
    }

    // Detection 

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRadius, _playerLayer);

        if (hits.Length > 0)
        {
            _playerInRange = true;
            if (_playerInventory == null)
                _playerInventory = FindFirstObjectByType<Inventory>();
        }
        else
        {
            _playerInRange = false;
            _playerInventory = null;
        }
    }

    // Pickup 

    private void TryPickup()
    {
        if (_playerInventory == null) return;

        bool added = _playerInventory.AddItem(_item, _quantity);
        if (added)
            Destroy(gameObject);

    }
}