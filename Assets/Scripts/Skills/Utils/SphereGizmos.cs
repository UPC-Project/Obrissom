using UnityEngine;

public class RingIndicatorGizmo : MonoBehaviour
{
    [SerializeField] private float _range = 4f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _range);
    }
}