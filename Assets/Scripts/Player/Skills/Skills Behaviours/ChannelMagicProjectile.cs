using Obrissom.Enemy;
using Obrissom.Player;
using Obrissom.UI;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Channel_Magic_Projectile")]
public class ChannelMagicProjectile : SkillBehaviour
{
    [SerializeField] Vector3 _initialPosition = new Vector3();
    [SerializeField] float _speed = 4;
    [SerializeField] float _lifeTime = 1.5f;

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
        DPSCombat dpsCombat = caster.GetComponent<DPSCombat>();
        Vector3 spawnPosition = caster.transform.TransformPoint(_initialPosition);
        Vector3 directionToTarget = (targetPosition - spawnPosition).normalized;

        // needed bc ChannelMagicProjectile inherits from SkillBehaviour, can't inherit NetworkBehaviour
        dpsCombat.ChannelMagicProjectileServerRpc(spawnPosition, directionToTarget, skillData.minEffectValue, skillData.maxEffectValue, _speed, _lifeTime);
    }

    // called on dpsCombat
    public static void ExecuteOnServer(GameObject caster, Vector3 spawnPosition, Vector3 directionToTarget, int minDamage, int maxDamage, float speed, float lifeTime)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();

        ProjectileTrigger trigger = MagicProjectilePool.Instance.Get(spawnPosition);
        GameObject projectile = trigger.gameObject;

        projectile.SetActive(true);

        Coroutine travel = playerCombat.StartCoroutine(SpellTravel(projectile, directionToTarget, trigger, speed, lifeTime));

        // OnHit will be called on ProjectileTrigger
        trigger.ClearSubscriptions();
        trigger.OnHit += (col) =>
        {

            if (col.transform.root.CompareTag("Enemy"))
            {
                EnemyBase enemy = col.transform.root.GetComponent<EnemyBase>();
                var (magicDamage, isCritic) = playerCombat.CalculateMagicDamage(minDamage, maxDamage);
                NetworkObject netObj = caster.GetComponent<NetworkObject>();
                enemy.TakeDamageRpc(magicDamage, EffectType.MagicDamage, isCritic, col.transform.position, netObj);
            }
            playerCombat.StopCoroutine(travel);
            MagicProjectilePool.Instance.Return(trigger);
        };
    }

    private static IEnumerator SpellTravel(GameObject projectile, Vector3 direction, ProjectileTrigger trigger, float speed, float lifeTime)
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
