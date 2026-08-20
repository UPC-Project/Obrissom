using UnityEngine;
using UnityEngine.EventSystems;

namespace Obrissom.UI
{
    public class SkillsTooltipDisplayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        [SerializeField] private Skill _slotSkill = null;
        [SerializeField] public SkillKey slotKey;

        public void SetSkill(Skill skill) => _slotSkill = skill;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_slotSkill == null) return;

            SkillTooltip.Instance.ShowTooltip(_slotSkill, slotKey);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SkillTooltip.Instance.HideTooltip();
        }
    }
}