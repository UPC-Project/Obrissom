using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public float xp;
    public float xpNeeded; // XP needed for next level
    public int currentLevel = 1;
    
    [Header("Components")]
    private PlayerCombat _playerCombat;


    private void Start()
    {
        _playerCombat = GetComponent<PlayerCombat>();
        // TODO: save information
        // Load player data from file
        xpNeeded = LevelUpRequirements.LevelRequirements[currentLevel];
    }
    public void GainXP(float amount)
    {
        if (currentLevel >= LevelUpRequirements.MAX_LEVEL) return;
        if (xp + amount >= xpNeeded)
        {
            float rest = (xp + amount) - xpNeeded;
            xp = rest;
            LevelUp();
        }
        else
        {
            xp += amount;
        }
    }

    [ContextMenu("Level Up")]
    private void LevelUp()
    {
        currentLevel++;
        xpNeeded = LevelUpRequirements.LevelRequirements[currentLevel];

        // Apply level up rewards
        // TODO: change by class
        LevelUpRewardsType rewards = LevelUpRewards.LevelRewardsDPS[currentLevel];
        ApplyRewards(rewards);
    }

    private void ApplyRewards(LevelUpRewardsType rewards)
    {
        foreach (var buf in rewards.Stats)
        {
            if (buf.Value.HasValue)
                _playerCombat.ApplyStat(buf.Key, buf.Value.Value);
        }

        foreach (Skill skill in rewards.NewSkills)
        {
            _playerCombat.UnlockSkill(skill);
        }
    }
}
