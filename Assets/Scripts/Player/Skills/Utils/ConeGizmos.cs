using UnityEngine;

public class SkillGizmos : MonoBehaviour
{
    // Temporary add this to player prefab and match settings with skill to check cone size
    [Header("Cone Settings")]
    [SerializeField] private float range = 2.5f;
    [SerializeField] private float angle = 55f;
    [SerializeField] private float originOffset = 0.5f;

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 0.9f - transform.forward * originOffset;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(origin, range);

        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * range;
        Vector3 leftBound = Quaternion.Euler(0, -angle / 2f, 0) * forward;
        Vector3 rightBound = Quaternion.Euler(0, angle / 2f, 0) * forward;
        Vector3 upBound = Quaternion.Euler(-angle / 2f, 0, 0) * forward;
        Vector3 downBound = Quaternion.Euler(angle / 2f, 0, 0) * forward;

        Gizmos.DrawRay(origin, forward);
        Gizmos.DrawRay(origin, leftBound);
        Gizmos.DrawRay(origin, rightBound);
        Gizmos.DrawRay(origin, upBound);
        Gizmos.DrawRay(origin, downBound);
    }
}
