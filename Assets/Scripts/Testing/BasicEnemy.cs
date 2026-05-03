using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Renderer))]
public class BasicEnemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Drop")]
    [SerializeField] private Item _dropItem;
    [SerializeField] private int _dropQuantity = 1;
    [SerializeField] private GameObject _worldItemPrefab;

    [Header("Hit Flash")]
    [SerializeField] private float _flashDuration = 0.15f;
    [SerializeField] private Color _hitColor = Color.red;

    private float _currentHealth;
    private Renderer _renderer;
    private Color _originalColor;
    private float _flashTimer;
    private bool _isFlashing;

    private void Start()
    {
        _currentHealth = _maxHealth;
        _renderer = GetComponent<Renderer>();
        _originalColor = _renderer.material.color;
    }

    private void Update()
    {
        if (_isFlashing)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _renderer.material.color = _originalColor;
                _isFlashing = false;
            }
        }

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            TakeDamage(10f);
        }
    }

  
    [ContextMenu("Forzar Daño Manual")]
    public void TestDamage()
    {
        TakeDamage(10f);
    }

    private void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        Debug.Log($"Enemigo dañado. Vida restante: {_currentHealth}");

        
        _renderer.material.color = _hitColor;
        _flashTimer = _flashDuration;
        _isFlashing = true;

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        DropItem();
        Destroy(gameObject);
    }

    private void DropItem()
    {
        if (_dropItem == null || _worldItemPrefab == null) return;

        Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
        GameObject dropped = Instantiate(_worldItemPrefab, dropPosition, Quaternion.identity);

        WorldItem worldItem = dropped.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.item = _dropItem;
            worldItem.quantity = _dropQuantity;
        }
    }
}