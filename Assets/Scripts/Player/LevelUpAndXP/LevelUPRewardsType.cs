using System.Collections.Generic;

// Type
public class LevelUpRewardsType
{
    public Dictionary<StatType, float?> Stats = new Dictionary<StatType, float?>();
    public List<Skill> NewSkills; // could be empty
}