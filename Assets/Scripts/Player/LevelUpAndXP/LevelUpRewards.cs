using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelUpRewards", menuName = "Game Logic/LevelUpRewards")]
public class LevelUpRewards : ScriptableObject
{
    public PlayerClass playerClass;
    public Skill basicSkill;
    public List<LevelReward> rewards;

    [System.Serializable]
    public class LevelReward
    {
        public int level;
        public Skill newSkill;
        public List<StatEntry> stats;
    }

    [System.Serializable]
    public class StatEntry
    {
        public Stats stat;
        public float value;
    }
}
