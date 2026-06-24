using Obrissom.Player;
using Obrissom.UI;
using System.Collections;
using Unity.Netcode.Components;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Teleport")]
public class Teleport : SkillBehaviour
{
    [SerializeField] private float _distanceLimit = 10f;
    [SerializeField] private float _tpIndicatorGroundElevation = 1f;

    private GameObject _tpIndicator;

    private GameObject GetTpIndicator()
    {
        if (_tpIndicator == null) _tpIndicator = DPSUIManager.Instance.GetTeleportCircle();
        return _tpIndicator;
    }

    public override void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject tpIndicator = GetTpIndicator();
        tpIndicator.SetActive(true);
    }
    
    // Changes tp circle indicator position relative to camera direction.
    // Updates each frame (called on PlayerSkills).
    public override void OnHoldUpdate(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject tpIndicator = GetTpIndicator();

        Vector3 casterPos = caster.transform.position;

        // Raycast to camera center
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        Vector3 directionXZ;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            directionXZ = new Vector3(hit.point.x - casterPos.x, 0f, hit.point.z - casterPos.z); // Ignores Y

            // hitpoint <= max range
            if (directionXZ.magnitude <= _distanceLimit)
            {
                Vector3 tpIndicatorPos = new Vector3(hit.point.x, hit.point.y + _tpIndicatorGroundElevation, hit.point.z);
                tpIndicator.transform.SetPositionAndRotation(tpIndicatorPos, Quaternion.FromToRotation(Vector3.forward, hit.normal));
                return;
            }
        }
        else
        {
            // Player probably looking at the sky (raycast not touching anything), we use projected direction XZ
            Vector3 camForwardXZ = new Vector3(Camera.main.transform.forward.x, 0f, Camera.main.transform.forward.z).normalized;
            directionXZ = camForwardXZ * _distanceLimit;
        }

        // Limit exceeded / player looking to high
        Vector3 limitXZ = casterPos + directionXZ.normalized * _distanceLimit;

        // Raycast to look down (at the ground)
        // +50 to elevate ray origin so we don't fall inside any object (for example an elevated plane /)
        Vector3 rayOrigin = new Vector3(limitXZ.x, casterPos.y + 50f, limitXZ.z);

        // Checks collison till 100 distance from ray origin
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 100f))
        {
            Vector3 tpIndicatorPos = new Vector3(groundHit.point.x, groundHit.point.y + _tpIndicatorGroundElevation, groundHit.point.z);
            tpIndicator.transform.SetPositionAndRotation(tpIndicatorPos, Quaternion.FromToRotation(Vector3.forward, groundHit.normal));
        }
        else
        {
            // Fallback, collider not found
            Vector3 fallbackPos = new Vector3(limitXZ.x, casterPos.y + _tpIndicatorGroundElevation, limitXZ.z); // horizontal determined by player camera
            tpIndicator.transform.SetPositionAndRotation(fallbackPos, Quaternion.Euler(-90f,0,0));
        }
    }

    public override void OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject tpIndicator = GetTpIndicator();
        Vector3 destination = tpIndicator.transform.position;
        tpIndicator.SetActive(false);

        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        playerCombat.StartCoroutine(CastTeleport(caster, playerCombat, destination, skillData.castTime));
    }

    private IEnumerator CastTeleport(GameObject caster, PlayerCombat playerCombat, Vector3 destination, float castTime)
    {
        playerCombat.SetInvulnerable(true);
        // TODO: trigger casting animation
        Debug.Log("casting tp...");

        yield return new WaitForSeconds(castTime);

        caster.GetComponent<CharacterController>().enabled = false;

        if (caster.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(destination, caster.transform.rotation, caster.transform.localScale);
        }
        else
        {
            caster.transform.position = destination;
        }

        caster.GetComponent<CharacterController>().enabled = true;
        Debug.Log("tp casted");

        playerCombat.SetInvulnerable(false);
    }

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition) { }

}
