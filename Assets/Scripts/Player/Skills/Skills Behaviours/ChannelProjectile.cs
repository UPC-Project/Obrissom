using Obrissom.Player;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Channel_Projectile")]
public class ChannelProjectile : SkillBehaviour
{
    [SerializeField] string _projectileName = "MagicOrb_Projectile"; // should be a player child
    [SerializeField] Vector3 initialPosition = new Vector3();
    [SerializeField] float speed = 4;
    [SerializeField] float lifeTime = 1.5f; // should be lower or equal than skill cooldown
    [SerializeField] float slopAngleTol = 45f;


    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        GameObject projectile = caster.transform.Find(_projectileName).gameObject;
        projectile.transform.position = caster.transform.TransformPoint(initialPosition);
        projectile.SetActive(true);

        Coroutine travel = playerCombat.StartCoroutine(SpellTravel(projectile, caster.transform.forward, caster));

        // OnHit will be called on projectile trigger
        var trigger = projectile.GetComponent<ProjectileTrigger>();
        trigger.ClearSubscriptions();
        trigger.OnHit += (other) =>
        {
            var (magicDamage, isCritic) = playerCombat.CalculateMagicDamage(skillData.minMagicDamage, skillData.maxMagicDamage);
            other.GetComponent<TestEnemy>()?.TakeDamage(magicDamage, DamageType.MagicDamage, isCritic, other.transform.position);
            playerCombat.StopCoroutine(travel);
            projectile.SetActive(false);
        };
    }

    IEnumerator SpellTravel(GameObject projectile, Vector3 direction, GameObject caster)
    {
        Vector3 initDir = new Vector3(direction.x, 0f, direction.z).normalized;
        float elapsed = 0f;
        while (elapsed < lifeTime)
        {
            projectile.transform.position += initDir * Time.deltaTime * speed;

            // follows terrain height
            if (Physics.Raycast(projectile.transform.position, Vector3.down, out RaycastHit hit, 10f))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > slopAngleTol) // probably a wall / too high
                {
                    projectile.SetActive(false);
                    yield break; 
                }

                float aboveGroundY = hit.point.y + initialPosition.y;
                projectile.transform.position = new Vector3(
                    projectile.transform.position.x,
                    aboveGroundY,
                    projectile.transform.position.z
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        projectile.SetActive(false);
    }
}
