using Obrissom.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Area_Heal")]
public class AreaHeal : SkillBehaviour
{
    [SerializeField] private float _healCircleRange = 5;
    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();

        playerCombat.StartCoroutine(HealOverTime(playerCombat, skillData, caster));
    }

    // Only player inside the area will recive healing
    private IEnumerator HealOverTime(PlayerCombat playerCombat, Skill skillData, GameObject caster)
    {
        // initial base heal
        // caster also recives instant healing.
        var (baseHeal, baseIsCritic) = playerCombat.CalculateHeal(skillData.minEffectValue, skillData.maxEffectValue);
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, _healCircleRange);
        HashSet<PlayerCombat> players = new HashSet<PlayerCombat>();
        foreach (Collider hit in hits)
        {
            if (!hit.transform.root.CompareTag("Player")) continue;

            PlayerCombat player = hit.transform.root.GetComponent<PlayerCombat>();
            if (player == null || players.Contains(player)) continue;
            players.Add(player);
            player.HealRpc(baseHeal);
        }

        // over time heal
        // caster doesn't recives healing over time.
        float elapsed = 0f;
        while (elapsed < skillData.damagePerSecondTime)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            var (heal, isCritic) = playerCombat.CalculateHeal(skillData.minDamagePerSecond, skillData.maxDamagePerSecond);

            hits = Physics.OverlapSphere(caster.transform.position, _healCircleRange);
            players.Clear();
            foreach (Collider hit in hits)
            {
                if (!hit.transform.root.CompareTag("Player")) continue;

                PlayerCombat player = hit.transform.root.GetComponent<PlayerCombat>();
                if (player == null || players.Contains(player) || player == playerCombat) continue;
                players.Add(player);
                player.HealRpc(heal);
            }
        }
    }
}
