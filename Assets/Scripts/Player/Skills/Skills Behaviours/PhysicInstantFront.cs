using Obrissom.Enemy;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Physic_Instant_Front")]
public class PhysicInstantFront : SkillBehaviour
{
    [SerializeField] private float range = 2.5f; // large range
    [SerializeField] private float angle = 55f; // cone angle

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        DPSCombat dpsCombat = caster.GetComponent<DPSCombat>();
        dpsCombat.PhysicInstantFrontServerRpc(skillData.minPhysicDamage, skillData.maxPhysicDamage, range, angle);
    }

    // called on dpsCombat
    public static void ExecuteOnServer(GameObject caster, int minDamage, int maxDamage, float range, float angle)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();

        Vector3 origin = caster.transform.position + Vector3.up * 0.9f;
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, range);

        foreach (Collider hit in hits)
        {
            if (!hit.transform.root.CompareTag("Enemy")) continue;

            Vector3 directionToTarget = (hit.transform.position - origin).normalized;
            float angleToTarget = Vector3.Angle(caster.transform.forward, directionToTarget);

            if (angleToTarget <= angle / 2f)
            {
                EnemyBase enemy = hit.transform.root.GetComponent<EnemyBase>();
                var (physicDamage, isCriticPhysic) = playerCombat.CalculatePhysicalDamage(minDamage, maxDamage);
                NetworkObject netObj = caster.GetComponent<NetworkObject>();
                if (physicDamage != 0) enemy.TakeDamagRpc(physicDamage, DamageType.PhysicDamage, isCriticPhysic, hit.transform.position, netObj);
            }
        }
    }
}