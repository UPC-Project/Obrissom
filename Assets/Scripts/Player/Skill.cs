using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public string description;
    [Min(0)] public float cooldownTime;
    [Min(0)] public int damage;
    [Min(0)] public int heal;
    [Min(0)] public int cost; // mana/fury
    // asssociated button?

}
