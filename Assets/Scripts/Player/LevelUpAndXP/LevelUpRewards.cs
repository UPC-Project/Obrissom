using System.Collections.Generic;

// CONSTANT
public class LevelUp
{
    public Dictionary<int, LevelUPRewardsType> LevelRewardsDPS = new Dictionary<int, LevelUPRewardsType>()
    {
        { 1, new LevelUPRewardsType {
                PermanentBufs = {
                    { "attackBoost", null},
                    { "mitigateDamage", null},
                    { "speedBoost", null},
                    { "moreHealth", null},
                    {"criticPercentage", null},
                },
             NewSkills = new List<Skill>() }
        },
        { 2, new LevelUPRewardsType {
                PermanentBufs = {
                    { "attackBoost", null},
                    { "mitigateDamage", null},
                    { "speedBoost", null},
                    { "moreHealth", null},
                    {"criticPercentage", null},
                },
             NewSkills = new List<Skill>() }
        },
        { 3, new LevelUPRewardsType {
                PermanentBufs = {
                    { "attackBoost", null},
                    { "mitigateDamage", null},
                    { "speedBoost", null},
                    { "moreHealth", null},
                    {"criticPercentage", null},
                },
             NewSkills = new List<Skill>() }
        },
        { 4, new LevelUPRewardsType {
                PermanentBufs = {
                    { "attackBoost", null},
                    { "mitigateDamage", null},
                    { "speedBoost", null},
                    { "moreHealth", null},
                    {"criticPercentage", null},
                },
             NewSkills = new List<Skill>() }
        }
    };


    // TODO: when classes are implemented

    //public Dictionary<int, LevelUPRewardsType> LevelRewardsTank = new Dictionary<int, LevelUPRewardsType>()
    //{
    //    { 1, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 2, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 3, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 4, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    }
    //};


    //public Dictionary<int, LevelUPRewardsType> LevelRewardsHealer = new Dictionary<int, LevelUPRewardsType>()
    //{
    //    { 1, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 2, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 3, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    },
    //    { 4, new LevelUPRewardsType {
    //            permanentBufs = {
    //                { "attackBoost", null},
    //                { "mitigateDamage", null},
    //                { "speedBoost", null},
    //                { "moreHealth", null},
    //                {"criticPercentage", null},
    //            },
    //         newSkills = new List<Skill>() }
    //    }
    //};
}