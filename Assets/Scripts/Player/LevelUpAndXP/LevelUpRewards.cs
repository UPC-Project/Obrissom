using System.Collections.Generic;

// CONSTANT
public class LevelUpRewards
{
    public static readonly Dictionary<int, LevelUpRewardsType> LevelRewardsDPS = new Dictionary<int, LevelUpRewardsType>()
    {
        { 2, new LevelUpRewardsType {
                Stats = {
                    { StatType.MoreHealth, 10 },
                    { StatType.MoreResource, 20 },
                    { StatType.AttackBoost, null },
                    { StatType.MitigateDamage, null },
                    { StatType.SpeedBoost, null },
                    { StatType.CriticPercentage, null },
                },
             NewSkills = new List<Skill>() }
        },
        { 3, new LevelUpRewardsType {
                Stats = {
                    { StatType.MoreHealth, 10 },
                    { StatType.MoreResource, 20 },
                    { StatType.AttackBoost, 1.10f },
                    { StatType.MitigateDamage, null },
                    { StatType.SpeedBoost, null },
                    { StatType.CriticPercentage, 0.15f },
                },
             NewSkills = new List<Skill>() }
        },
        { 4, new LevelUpRewardsType {
                Stats = {
                    { StatType.MoreHealth, null },
                    { StatType.MoreResource, null },
                    { StatType.AttackBoost, null },
                    { StatType.MitigateDamage, null },
                    { StatType.SpeedBoost, null },
                    { StatType.CriticPercentage, null },
                },
             NewSkills = new List<Skill>() }
        },
        { 5, new LevelUpRewardsType {
                Stats = {
                    { StatType.MoreHealth, null },
                    { StatType.MoreResource, null },
                    { StatType.AttackBoost, null },
                    { StatType.MitigateDamage, null },
                    { StatType.SpeedBoost, null },
                    { StatType.CriticPercentage, null },
                },
             NewSkills = new List<Skill>() }
        }
    };


    // TODO: when classes are implemented

    //public static readonly Dictionary<int, LevelUPRewardsType> LevelRewardsTank = new Dictionary<int, LevelUPRewardsType>()
    //{
    //    { 2, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 3, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 4, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 5, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    }
    //};


    //public static readonly Dictionary<int, LevelUPRewardsType> LevelRewardsHealer = new Dictionary<int, LevelUPRewardsType>()
    //{
    //    { 2, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 3, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 4, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 5, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "moreHealth", null},
    //                { "moreResource", null},
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    }
    //};
}