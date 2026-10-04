using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    public class SkillsUI : MonoBehaviour
    {
        [SerializeField] private SkillsTooltipDisplayer _lbSlot;
        [SerializeField] private SkillsTooltipDisplayer _skill1Slot;
        [SerializeField] private SkillsTooltipDisplayer _skill2Slot;
        [SerializeField] private SkillsTooltipDisplayer _skill3Slot;
        [SerializeField] private SkillsTooltipDisplayer _skill4Slot;

        [SerializeField] private Image _lbSprite;
        [SerializeField] private Image _skill1Sprite;
        [SerializeField] private Image _skill2Sprite;
        [SerializeField] private Image _skill3Sprite;
        [SerializeField] private Image _skill4Sprite;

        private Dictionary<SkillKey, SkillsTooltipDisplayer> _slots;
        private Dictionary<SkillKey, Image> _slotsImage;

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

            _slotsImage = new Dictionary<SkillKey, Image>
        {
            { SkillKey.LB, _lbSprite },
            { SkillKey.ONE, _skill1Sprite },
            { SkillKey.TWO, _skill2Sprite },
            { SkillKey.THREE, _skill3Sprite },
            { SkillKey.FOUR, _skill4Sprite },
        };
        }

        public void OnSkillUnlocked(SkillKey key, Skill skill)
        {
            if (_slots.TryGetValue(key, out SkillsTooltipDisplayer slot))
            {
                slot.SetSkill(skill);
                slot.slotKey = key;
            }
            if (_slotsImage.TryGetValue(key, out Image image))
            {
                image.sprite = skill.skillImage;
            }
        }
    }
}