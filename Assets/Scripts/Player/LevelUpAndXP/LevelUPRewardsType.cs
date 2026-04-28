using System.Collections.Generic;

// Type
public class LevelUpRewardsType
{
    public Dictionary<Stats, float?> Stats = new Dictionary<Stats, float?>();
    public List<Skill> NewSkills; // could be empty
}