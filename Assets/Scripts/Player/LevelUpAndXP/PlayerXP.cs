using Unity.Netcode;
using UnityEngine;
using static LevelUpRewards;

namespace Obrissom.Player
{
    public class PlayerXP : NetworkBehaviour
    {
        public float xp;
        [Tooltip("XP needed for next level")]
        public float xpNeeded; // 
        [Range(1, 5)] public int currentLevel = 1;

        [Header("Components")]
        [SerializeField] private LevelUpRewards _levelUpRewards; // Depends on Player Class
        private UI.LevelAndXPUI _XpUi;
        private PlayerSkills _playerSkills;
        private PlayerStats _playerStats;


        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            _playerSkills = GetComponent<PlayerSkills>();
            _playerStats = GetComponent<PlayerStats>();
            _XpUi = UI.PlayerUIManager.Instance.GetLevelAndXPUI();
            xpNeeded = LevelUpRequirements.LevelRequirements[currentLevel];
            _XpUi.UpdateXP(xp, xpNeeded, currentLevel);

            // Player intiialize always with basic skill unlocked
            _playerSkills.UnlockSkill(_levelUpRewards.basicSkill);
        }

        public void GainXP(float amount)
        {
            if (!IsOwner || currentLevel >= LevelUpRequirements.MAX_LEVEL) return;

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

            _XpUi.UpdateXP(xp, xpNeeded, currentLevel);
        }

        [ContextMenu("Level Up")]
        private void LevelUp()
        {
            currentLevel++;
            xpNeeded = LevelUpRequirements.LevelRequirements[currentLevel];

            // Depends on player class and level
            LevelUpRewards.LevelReward rewards = _levelUpRewards.rewards.Find(r => r.level == currentLevel);
            ApplyRewards(rewards);
        }

        private void ApplyRewards(LevelReward rewards)
        {
            foreach (var stat in rewards.stats)
            {
                _playerStats.AddStat(stat.stat, stat.value);
            }

            if (rewards.newSkill != null)
            {
                _playerSkills.UnlockSkill(rewards.newSkill);
            }
        }
    }

}