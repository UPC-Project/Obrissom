using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Instant_and_DOT_Damage")]
public class InstantAndDOTDamage : SkillBehaviour
{
    [Header("Weapon")]
    [Tooltip("true = damages all enemies the collider passes through.\nfalse = only damages the first enemy hit and then stops.")]
    [SerializeField] private bool _weaponHitMultipleEnemies = false;
    [SerializeField] private bool _weaponInstantEffect = false;
    [SerializeField] private bool _weaponDOT = false;

    [Header("Skill Zone")]
    [Tooltip("true = damages all enemies the collider passes through.\nfalse = only damages the first enemy hit and then stops.")]
    [SerializeField] private bool _skillZoneHitMultipleEnemies = false;
    [SerializeField] private bool _skillZoneInstantEffect = false;
    [SerializeField] private bool _skillZoneDOT = false;


    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        var hitboxes = caster.GetComponentsInChildren<WeaponSkillHitbox>(true);
        foreach (var hitbox in hitboxes)
        {
            if (hitbox.hitboxType == SkillHitboxType.Weapon)
            {
                hitbox.SetupSkill(skillData, _weaponHitMultipleEnemies, _weaponInstantEffect, _weaponDOT);
            }
            else if (hitbox.hitboxType == SkillHitboxType.SkillZone)
            {
                hitbox.SetupSkill(skillData, _skillZoneHitMultipleEnemies, _skillZoneInstantEffect, _skillZoneDOT);

            }
        }
    }

}
