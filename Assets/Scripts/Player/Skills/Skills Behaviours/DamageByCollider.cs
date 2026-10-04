using UnityEngine;

/// Activates the DamageSkillHitbox on the player's weapon.
[CreateAssetMenu(menuName = "Skills/Behaviours/Damage_By_Collider")]
public class DamageByCollider : SkillBehaviour
{
    [SerializeField] private SkillHitboxType _hitboxType = SkillHitboxType.Weapon;
    [Tooltip("true = damages all enemies the collider passes through.\nfalse = only damages the first enemy hit and then stops.")]
    [SerializeField] private bool _hitMultipleEnemies = false;

    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        var hitboxes = caster.GetComponentsInChildren<DamageSkillHitbox>(true);
        foreach (var hitbox in hitboxes)
        {
            if (hitbox.hitboxType == _hitboxType)
            {
                hitbox.SetupCollider(skillData, _hitMultipleEnemies, true, false);
            }
        }
    }
}
