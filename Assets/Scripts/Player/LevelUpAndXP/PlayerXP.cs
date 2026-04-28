using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerXP : NetworkBehaviour
    {
        public float xp;
        [Tooltip("XP needed for next level")]
        public float xpNeeded; // 
        public int currentLevel = 1;

        [Header("Components")]
        private LevelAndXPUI _XpUi;
        private PlayerSkills _playerSkills;
        private PlayerStats _playerStats;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            _playerSkills = GetComponent<PlayerSkills>();
            _playerStats = GetComponent<PlayerStats>();
            _XpUi = PlayerUIManager.Instance.GetLevelAndXPUI();
            xpNeeded = LevelUpRequirements.LevelRequirements[currentLevel];
            _XpUi.UpdateXP(xp, xpNeeded, currentLevel);
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

            // Update  UI
            _XpUi.UpdateXP(xp, xpNeeded, currentLevel);


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
            foreach (var stat in rewards.Stats)
            {
                if (stat.Value.HasValue)
                    _playerStats.AddStat(stat.Key, stat.Value.Value);
            }

            foreach (Skill skill in rewards.NewSkills)
            {
                _playerSkills.UnlockSkill(skill);
            }
        }
    }

}