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
    [Min(0), Tooltip("If equal 0 then not applied")] public int minDamagePerSecond;
    [Min(0), Tooltip("If equal 0 then not applied")] public int maxDamagePerSecond;
    [Min(0), Tooltip("If equal 0 then not applied")] public int damagePerSecondTime;
    public EffectType damagePerSecondType;

    [Header("Physical Damage | Magic Damage | Heal - Value")]
    [Min(0)] public int minEffectValue;
    [Min(0)] public int maxEffectValue;
    public EffectType effectType;

    public SkillBehaviour behaviour;
}