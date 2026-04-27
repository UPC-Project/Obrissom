using System.Collections.Generic;

// CONSTANT
public class LevelUpRewards
{
    public static readonly Dictionary<int, LevelUpRewardsType> LevelRewardsDPS = new Dictionary<int, LevelUpRewardsType>()
    {
        { 2, new LevelUpRewardsType {
                Stats = {
                    [Stats.BonusHealth] = 10f,
                    [Stats.BonusResource] = 20f,
                },
             NewSkills = new List<Skill>() }
        },
        { 3, new LevelUpRewardsType {
                Stats = {
                    [Stats.BonusHealth] = 10f,
                    [Stats.BonusResource] = 20f,
                    [Stats.BonusPhysicalAttack] = 1f,
                    [Stats.CriticalChance] = 0.1f,
                    [Stats.CriticalDamage] = 0.25f,
                },
             NewSkills = new List<Skill>() }
        },
        { 4, new LevelUpRewardsType {
                Stats = {
                      [Stats.BonusHealth] = 5f,
                      [Stats.BonusResource] = 30f,
                      [Stats.BonusMagicAttack] = 1f,
                      [Stats.CriticalChance] = 0.1f,
                },
             NewSkills = new List<Skill>() }
        },
        { 5, new LevelUpRewardsType {
                Stats = {
                     [Stats.BonusHealth] = 5f,
                     [Stats.BonusResource] = 40f,
                     [Stats.BonusPhysicalAttack] = 1f,
                     [Stats.BonusMagicAttack] = 2f,
                     [Stats.MagicAttackMultiplier] = 0.3f,
                     [Stats.CriticalChance] = 0.1f,
                },
             NewSkills = new List<Skill>() }
        }
    };

}
// TODO: rewards for all classes