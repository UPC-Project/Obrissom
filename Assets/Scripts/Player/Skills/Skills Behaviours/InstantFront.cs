using Obrissom.Player;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Instant_Front")]
public class InstantFront : SkillBehaviour
{
    [SerializeField] private float range = 3f; // large range
    [SerializeField] private float angle = 60f; // cone angle


    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        
        Vector3 origin = caster.transform.position + Vector3.up * 0.9f;
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, range);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Vector3 directionToTarget = (hit.transform.position - origin).normalized;
            float angleToTarget = Vector3.Angle(caster.transform.forward, directionToTarget);

            if (angleToTarget <= angle / 2f)
            {
                var (physicDamage, isCriticPhysic) = playerCombat.CalculatePhysicalDamage(skillData.minPhysicDamage,skillData.maxPhysicDamage);
                var (magicDamage, isCriticMagic) = playerCombat.CalculateMagicDamage(skillData.minMagicDamage, skillData.maxMagicDamage);

                if (physicDamage!=0) hit.GetComponent<TestEnemy>()?.TakeDamage(physicDamage, DamageType.PhysicDamage, isCriticPhysic, hit.transform.position);
                if (magicDamage!=0) hit.GetComponent<TestEnemy>()?.TakeDamage(magicDamage, DamageType.MagicDamage, isCriticMagic, hit.transform.position);
            }
        }
    }
}