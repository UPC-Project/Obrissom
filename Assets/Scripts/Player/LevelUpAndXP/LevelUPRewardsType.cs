using System.Collections.Generic;

// Type
public class LevelUPRewardsType
{
    public Dictionary<string, float?> PermanentBufs = new Dictionary<string, float?>()
    {
        // null if the level up doesn't give that buf
        { "attackBoost", null},
        { "mitigateDamage", null},
        { "speedBoost", null},
        { "moreHealth", null},
        {"criticPercentage", null},
    };
    public List<Skill> NewSkills; // could be empty
}