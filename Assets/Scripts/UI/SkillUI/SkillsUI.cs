using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.UI
{
    public class SkillsUI : MonoBehaviour
    {
        [SerializeField] private SkillsTooltipDisplayer _lbSlot;
        [SerializeField] private SkillsTooltipDisplayer _skill1Slot;
        [SerializeField] private SkillsTooltipDisplayer _skill2Slot;
        [SerializeField] private SkillsTooltipDisplayer _skill3Slot;
        [SerializeField] private SkillsTooltipDisplayer _skill4Slot;

        private Dictionary<SkillKey, SkillsTooltipDisplayer> _slots;

        private void Awake()
        {
            _slots = new Dictionary<SkillKey, SkillsTooltipDisplayer>
        {
            { SkillKey.LB, _lbSlot },
            { SkillKey.ONE, _skill1Slot },
            { SkillKey.TWO, _skill2Slot },
            { SkillKey.THREE, _skill3Slot },
            { SkillKey.FOUR, _skill4Slot },
        };
        }

        public void OnSkillUnlocked(SkillKey key, Skill skill)
        {
            if (_slots.TryGetValue(key, out SkillsTooltipDisplayer slot))
            {
                slot.SetSkill(skill);
                slot.slotKey = key;
            }
        }
    }
}