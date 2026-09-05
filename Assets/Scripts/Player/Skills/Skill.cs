using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    [Tooltip("Name will be shown to the player")] public string skillName;
    [Tooltip("Image shown on HUD")] public Sprite skillImage;
    [Tooltip("Description will be shown to the player")] public string description;
    [Min(0), Tooltip("How long until player can activates skill again")] public float cooldownTime;
    [Min(0), Tooltip("mana/stamina/fury cost")] public int cost; // mana/fury
    [Min(0), Tooltip("How long until player executes skill behaviour after skill activation")] public int castTime; // depends on the animation + preference

    [Header("Effect")]
    [Min(0)] public int minEffectValue;
    [Min(0)] public int maxEffectValue;
    public EffectType effectType;

    [Header("Effect Per Second")]
    [Min(0), Tooltip("If equal 0 then not applied")] public int minDamagePerSecond;
    [Min(0), Tooltip("If equal 0 then not applied")] public int maxDamagePerSecond;
    [Min(0), Tooltip("If equal 0 then not applied, int type because is effect per second")] public int damagePerSecondTime;
    public EffectType damagePerSecondType;


    public SkillBehaviour behaviour;
}