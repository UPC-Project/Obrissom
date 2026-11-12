using Obrissom.Player;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Invulnerable_Dash")]
public class InvulnerableDash : SkillBehaviour
{
    [SerializeField, Tooltip("dash duration in seconds")] private float _dashDuartion = 4f;
    [SerializeField, Tooltip("speed added while dashing")] private float _speedBonus = 3f;

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        PlayerStats playerStats = caster.GetComponent<PlayerStats>();
        playerCombat.StartCoroutine(Dash(playerCombat, playerStats));
    }

    private IEnumerator Dash(PlayerCombat playerCombat, PlayerStats playerStats)
    {
        playerCombat.SetInvulnerable(true);
        playerCombat.SetTargetable(false);
        playerStats.AddStat(Stats.BonusSpeed, _speedBonus);

        yield return new WaitForSeconds(_dashDuartion);

        playerStats.RemoveStat(Stats.BonusSpeed, _speedBonus);
        playerCombat.SetTargetable(true);
        playerCombat.SetInvulnerable(false);
    }
}

