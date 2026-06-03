using Obrissom.Player;
using Obrissom.UI;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Channel_Magic_Projectile")]
public class ChannelMagicProjectile : SkillBehaviour
{
    [SerializeField] Vector3 initialPosition = new Vector3();
    [SerializeField] float speed = 4;
    [SerializeField] float lifeTime = 1.5f;

    private GameObject _crosshair;

    private GameObject GetCrosshair()
    {
        if (_crosshair == null) _crosshair = DPSUIManager.Instance.GetCrosshair();
        return _crosshair;
    }

    public override void OnHold(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject crosshair = GetCrosshair();
        crosshair.SetActive(true);
    }

    public override void OnRelease(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        GameObject crosshair = GetCrosshair();
        crosshair.SetActive(false);
        Execute(caster, skillData, targetPosition);
    }

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        ProjectileTrigger trigger = MagicProjectilePool.Instance.Get(initialPosition);
        GameObject projectile = trigger.gameObject;
        projectile.transform.position = caster.transform.TransformPoint(initialPosition);
        Vector3 directionToTarget = (targetPosition - caster.transform.TransformPoint(initialPosition)).normalized;
        float angleDown = Vector3.Angle(directionToTarget, Vector3.up) - 90f;

        projectile.SetActive(true);

        Coroutine travel = playerCombat.StartCoroutine(SpellTravel(projectile, directionToTarget, trigger));

        // OnHit will be called on projectile trigger
        trigger.ClearSubscriptions();
        trigger.OnHit += (other) =>
        {
            var (magicDamage, isCritic) = playerCombat.CalculateMagicDamage(skillData.minMagicDamage, skillData.maxMagicDamage);
            other.GetComponent<TestEnemy>()?.TakeDamage(magicDamage, DamageType.MagicDamage, isCritic, other.transform.position);
            playerCombat.StopCoroutine(travel);
            MagicProjectilePool.Instance.Return(trigger);
        };
    }
    IEnumerator SpellTravel(GameObject projectile, Vector3 direction, ProjectileTrigger trigger)
    {
        float elapsed = 0f;
        while (elapsed < lifeTime)
        {
            projectile.transform.position += direction * Time.deltaTime * speed;
            elapsed += Time.deltaTime;
            yield return null;
        }
        MagicProjectilePool.Instance.Return(trigger);
    }

}
