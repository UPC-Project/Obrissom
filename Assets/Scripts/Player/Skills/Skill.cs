using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public string description;
    [Min(0)] public float cooldownTime;
    [Min(0)] public int cost; // mana/fury

    [Header("Damage Per Second")]
    [Min(0)] public int damagePerSecond;
    [Min(0)] public int damagePerSecondTime;
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