using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public string description;
    [Min(0)] public float cooldownTime;
    [Min(0)] public int physicDamage;
    [Min(0)] public int magicDamage;
    [Min(0)] public int heal;
    [Min(0)] public int cost; // mana/fury
    [Min(0)] public int damagePerSecond;
    [Min(0)] public int damagePerSecondTime;

    public SkillBehaviour behaviour;
}