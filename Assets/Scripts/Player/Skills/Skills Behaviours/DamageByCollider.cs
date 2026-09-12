using UnityEngine;

/// Activates the WeaponSkillHitbox on the player's weapon..
[CreateAssetMenu(menuName = "Skills/Behaviours/Damage_By_Collider")]
public class DamageByCollider : SkillBehaviour
{
    [Tooltip("true = damages all enemies the collider passes through.\nfalse = only damages the first enemy hit and then stops.")]
    public bool hitMultipleEnemies = false;

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        WeaponSkillHitbox hitbox = caster.GetComponentInChildren<WeaponSkillHitbox>(true);
        hitbox.SetupSkill(skillData, hitMultipleEnemies);
    }
}

