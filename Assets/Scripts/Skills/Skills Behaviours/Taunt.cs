using Obrissom.Player;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Taunt")]
public class Taunt : SkillBehaviour
{
    [SerializeField] private float _radius = 10f;
    [SerializeField] private float _tauntDuration = 5f;
    [SerializeField] private float _showCircleTime = 2f; // could be any other effect, also instead of a fixed time could be while the user holds the key

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        PlayerCombat playerCombat = caster.GetComponent<PlayerCombat>();
        playerCombat.StartCoroutine(ShowTauntCircle(playerCombat));
        var hitbox = caster.GetComponentInChildren<TauntHitBox>(true);
        hitbox.SetupCollider(_radius, _tauntDuration);
    }

    // The circle of this caster, seen by every player
    private IEnumerator ShowTauntCircle(PlayerCombat playerCombat)
    {
        playerCombat.SetSkillArea(SkillArea.TauntCircle, true);
        yield return new WaitForSecondsRealtime(_showCircleTime);
        playerCombat.SetSkillArea(SkillArea.TauntCircle, false);
    }
}
