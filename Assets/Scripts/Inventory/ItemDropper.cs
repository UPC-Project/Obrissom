using UnityEngine;

/// <summary>
/// Reusable service to spawn items in the world.
/// Used by both the player (drop from inventory) and enemies (drop on death).
/// Attach to the Player and to each enemy that drops items.
/// </summary>
public class ItemDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject _worldItemPrefab;
    [SerializeField] private float _dropForce = 3f;
    [SerializeField] private float _dropUpwardForce = 2f;


    /// <summary>
    /// Spawns an item in the world near whoever drops it.
    /// </summary>
    public void DropItem(Item item, int quantity = 1)
    {
        if (item == null) return;
        if (_worldItemPrefab == null)
        {
            return;
        }

        Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
        GameObject dropped = Instantiate(_worldItemPrefab, dropPosition, Random.rotation);

        WorldItem worldItem = dropped.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.item = item;
            worldItem.quantity = quantity;
        }

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = transform.forward * _dropForce + Vector3.up * _dropUpwardForce;
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}