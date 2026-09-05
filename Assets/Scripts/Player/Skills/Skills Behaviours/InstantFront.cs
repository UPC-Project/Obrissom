using Obrissom.Enemy;
using Obrissom.Player;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Instant_Front")]
public class InstantFront : SkillBehaviour
{
    [SerializeField] private float range = 2.5f; // large range
    [SerializeField] private float angle = 55f; // cone angle

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        // (Now it's only used by the dps, but this could be modularized for all the classes)
        // Mediator needed to work online
        DPSCombat dpsCombat = caster.GetComponent<DPSCombat>();
        dpsCombat.PhysicInstantFrontServerRpc(skillData.minEffectValue, skillData.maxEffectValue, range, angle, skillData.effectType, skillData.minDamagePerSecond, skillData.maxDamagePerSecond, skillData.damagePerSecondTime, skillData.damagePerSecondType);
    }

    // called on dpsCombat
    public static void ExecuteOnServer(GameObject caster, int minDamage, int maxDamage, float range, float angle, EffectType effect, int minDamagePerSecond, int maxDamagePerSecond, float damagePerSecondTime, EffectType damagePerSecondType)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();

        Vector3 origin = caster.transform.position + Vector3.up * 0.9f;
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, range);

        // Damage will affect every enemy inside the cone
        foreach (Collider hit in hits)
        {
            if (!hit.transform.root.CompareTag("Enemy")) continue;
            Vector3 directionToTarget = (hit.transform.position - origin).normalized;
            float angleToTarget = Vector3.Angle(caster.transform.forward, directionToTarget);

            if (angleToTarget <= angle / 2f)
            {
                var (damage, isCritic) = effect == EffectType.PhysicDamage
                    ? playerCombat.CalculatePhysicalDamage(minDamage, maxDamage)
                    : playerCombat.CalculateMagicDamage(minDamage, maxDamage);

                NetworkObject netObj = caster.GetComponent<NetworkObject>();

                EnemyBase enemy = hit.transform.root.GetComponent<EnemyBase>();

                if (minDamage != 0) enemy.TakeDamageRpc(damage, effect, isCritic, hit.transform.position, netObj);
                if (minDamagePerSecond != 0)
                    playerCombat.StartCoroutine(ApplyDamageOverTime(minDamagePerSecond, maxDamagePerSecond, damagePerSecondTime, damagePerSecondType, playerCombat, netObj, hit, enemy));
            }

        }
    }
    private static IEnumerator ApplyDamageOverTime(int minDamagePerSecond, int maxDamagePerSecond, float damagePerSecondTime, EffectType damagePerSecondType, PlayerCombat playerCombat, NetworkObject netObj, Collider hit, EnemyBase enemy)
    {
        float elapsed = 0f;
        while (elapsed < damagePerSecondTime)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            var (damage, isCritic) = damagePerSecondType == EffectType.PhysicDamage
                ? playerCombat.CalculatePhysicalDamage(minDamagePerSecond, maxDamagePerSecond)
                : playerCombat.CalculateMagicDamage(minDamagePerSecond, maxDamagePerSecond);

            enemy.TakeDamageRpc(damage, damagePerSecondType, isCritic, hit.transform.position, netObj);
        }
    }
}




