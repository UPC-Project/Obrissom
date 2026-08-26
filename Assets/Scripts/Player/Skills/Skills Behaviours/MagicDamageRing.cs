using Obrissom.Enemy;
using Obrissom.Player;
using Obrissom.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Magic_Damage_Ring")]
public class MagicDamageRing : SkillBehaviour
{
    [SerializeField] private float _distanceLimit = 10f;
    [SerializeField] private float _ringIndicatorGroundElevation = 1f;
    [SerializeField] private float _damageRingRange = 6f;
    [SerializeField] private LayerMask _groundLayers;

    private GameObject _ringIndicator;

    private GameObject GetRingIndicator()
    {
        if (_ringIndicator == null) _ringIndicator = DPSUIManager.Instance.GetMagicDamageRing();
        return _ringIndicator;
    }

    public override void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject ringIndicator = GetRingIndicator();
        ringIndicator.SetActive(true);
    }

    // Changes magic ring indicator position relative to camera direction.
    // Updates each frame (called on PlayerSkills).
    public override void OnHoldUpdate(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject ringIndicator = GetRingIndicator();

        Vector3 casterPos = caster.transform.position;

        // Raycast to camera center
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        Vector3 directionXZ;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, _groundLayers))
        {
            // player can only cast the skill on ground
            if (!hit.collider.CompareTag("Ground"))
            {
                ringIndicator.SetActive(false);
                return;
            }
            ringIndicator.SetActive(true);

            directionXZ = new Vector3(hit.point.x - casterPos.x, 0f, hit.point.z - casterPos.z); // Ignores Y

            // hitpoint <= max range
            if (directionXZ.magnitude <= _distanceLimit)
            {
                Vector3 tpIndicatorPos = new Vector3(hit.point.x, hit.point.y + _ringIndicatorGroundElevation, hit.point.z);
                ringIndicator.transform.SetPositionAndRotation(tpIndicatorPos, Quaternion.FromToRotation(Vector3.forward, hit.normal));
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
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 100f, _groundLayers))
        {
            Vector3 tpIndicatorPos = new Vector3(groundHit.point.x, groundHit.point.y + _ringIndicatorGroundElevation, groundHit.point.z);
            ringIndicator.transform.SetPositionAndRotation(tpIndicatorPos, Quaternion.FromToRotation(Vector3.forward, groundHit.normal));
        }
        else
        {
            // Fallback, collider not found
            Vector3 fallbackPos = new Vector3(limitXZ.x, casterPos.y + _ringIndicatorGroundElevation, limitXZ.z); // horizontal determined by player camera
            ringIndicator.transform.SetPositionAndRotation(fallbackPos, Quaternion.Euler(-90f, 0, 0));
        }
    }

    public override bool OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject ringIndicator = GetRingIndicator();
        if (!ringIndicator.activeSelf) return false;
        Vector3 ringAim = ringIndicator.transform.position;
        ringIndicator.SetActive(false);

        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        playerCombat.StartCoroutine(CastMagicRing(caster, playerCombat, ringAim, skillData));
        return true;
    }

    private IEnumerator CastMagicRing(GameObject caster, PlayerCombat playerCombat, Vector3 ringAim, Skill skillData)
    {
        // TODO: trigger casting animation
        // TODO: in a future don't block camera movement
        Debug.Log("casting magic ring...");
        PlayerLocomotionInput locomotionInput = caster.GetComponent<PlayerLocomotionInput>();
        PlayerCombatInput combatInput = caster.GetComponent<PlayerCombatInput>();
        PlayerController playerController = caster.GetComponent<PlayerController>();
        locomotionInput.enabled = false;
        combatInput.enabled = false;
        playerController.enabled = false;
        caster.GetComponent<CharacterController>().enabled = false;
        yield return new WaitForSeconds(skillData.castTime);
        locomotionInput.enabled = true;
        combatInput.enabled = true;
        playerController.enabled = true;
        caster.GetComponent<CharacterController>().enabled = true;
        Debug.Log("magic ring casted");

        float elapsed = 0f;
        while (elapsed < skillData.damagePerSecondTime)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            // all enemies still in the ring receive the same damage
            var (damage, isCritic) = playerCombat.CalculateMagicDamage(skillData.minDamagePerSecond, skillData.maxDamagePerSecond);

            Collider[] hits = Physics.OverlapSphere(ringAim, _damageRingRange);
            HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>(); // avoid hitting multiple times same enemy
            foreach (Collider hit in hits)
            {
                if (!hit.transform.root.CompareTag("Enemy")) continue;

                EnemyBase enemy = hit.transform.root.GetComponent<EnemyBase>();
                if (enemy == null || hitEnemies.Contains(enemy)) continue;
                hitEnemies.Add(enemy);

                NetworkObject netObj = caster.GetComponent<NetworkObject>();
                enemy.TakeDamageRpc(damage, skillData.damagePerSecondType, isCritic, hit.transform.position, netObj);
            }
        }
    }

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition) { }

}

