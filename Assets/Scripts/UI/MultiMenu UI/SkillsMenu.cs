using Obrissom.Player;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillsMenu : MenuPanel
{
    [Serializable]
    public class LongSkillSlotUI
    {
        public Image Icon;
        public TMP_Text Title;
        public TMP_Text Description;
        public TMP_Text Level;
    }

    [SerializeField] private List<LongSkillSlotUI> _skillSlots;

    public void BindPlayerXP(PlayerXP playerXp)
    {
        // player skills setup
        List<Skill> skills = new() { playerXp.LevelUpRewards.basicSkill };
        foreach (var reward in playerXp.LevelUpRewards.rewards)
        {
            if (reward.newSkill != null)
                skills.Add(reward.newSkill);
        }
        for (int i = 0; i < skills.Count; i++)
        {
            _skillSlots[i].Icon.sprite = skills[i].skillImage;
            _skillSlots[i].Title.text = skills[i].skillName;
            _skillSlots[i].Description.text = skills[i].description;
            _skillSlots[i].Level.text = "[ unlock at level " + (i + 1) + " ]";
        }
    }

}
