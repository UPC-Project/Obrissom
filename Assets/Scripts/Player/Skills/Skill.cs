using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    [Tooltip("name will be shown to the player")] public string skillName;
    [Tooltip("Description will be shown to the player")] public string description;
    [Min(0), Tooltip("How long until player can activates skill again")] public float cooldownTime;
    [Min(0), Tooltip("mana/stamina/fury cost")] public int cost; // mana/fury
    [Min(0), Tooltip("How long until player executes skill behaviour after skill activation")] public int castTime; // depends on the animation + preference

    [Header("Damage Per Second")]
    [Min(0), Tooltip("If equal 0 then not applied")] public int damagePerSecond;
    [Min(0), Tooltip("If equal 0 then not applied")] public int damagePerSecondTime;
    public DamageType damagePerSecondType;

    [Header("Physical Damage")]
    [Min(0)] public int minPhysicDamage;
    [Min(0)] public int maxPhysicDamage;

    [Header("Magic Damage")]
    [Min(0)] public int minMagicDamage;
    [Min(0)] public int maxMagicDamage;

    [Header("Heal")]
    [Min(0)] public int minHeal;
    [Min(0)] public int maxHeal;

    public SkillBehaviour behaviour;
}