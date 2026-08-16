using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.UI
{
    public class SkillTooltip : MonoBehaviour
    {
        public static SkillTooltip Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _skillName;
        [SerializeField] private TextMeshProUGUI _skillKey;
        [SerializeField] private TextMeshProUGUI _skillCost;
        [SerializeField] private TextMeshProUGUI _skillCooldown;
        [SerializeField] private TextMeshProUGUI _skillDescription;
        [SerializeField] private TextMeshProUGUI _skillValue;

        public Canvas parentCanvas;
        public Transform ToolTipTransform;

        private Dictionary<SkillKey, string> _keyNames;
        private Dictionary<EffectType, string> _effectNames;
        private Dictionary<EffectType, Color> _effectColors;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _keyNames = new Dictionary<SkillKey, string>
                {
                    { SkillKey.LB, "LB" },
                    { SkillKey.ONE, "1" },
                    { SkillKey.TWO, "2"},
                    { SkillKey.THREE, "3"},
                    { SkillKey.FOUR, "4"},
                };

            _effectNames = new Dictionary<EffectType, string>
                {
                    { EffectType.PhysicDamage, "Physical Damage" },
                    { EffectType.MagicDamage, "Magic Damage" },
                    { EffectType.Heal, "Heal"},
                };

            _effectColors = new Dictionary<EffectType, Color>
                {
                    { EffectType.PhysicDamage, new Color32(255,92,8,255)},
                    { EffectType.MagicDamage, new Color32(0,165,250,255)},
                    { EffectType.Heal, new Color32(255,224,88,255)},
                };
        }

        void Update()
        {
            if (gameObject.activeSelf)
            {
                Vector2 movePos;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, Mouse.current.position.ReadValue(), parentCanvas.worldCamera, out movePos);

                ToolTipTransform.position = parentCanvas.transform.TransformPoint(movePos);
            }
        }

        public void ShowTooltip(Skill skill, SkillKey key)
        {
            _skillName.text = skill.skillName;
            _skillKey.text = "[" + _keyNames[key] + "]";

            _skillCost.text = (skill.cost == 0) ? "No cost" : skill.cost.ToString()+ " Mana";
            _skillCooldown.text = skill.cooldownTime + "s Cooldown";

            _skillDescription.text = skill.description;

            _skillValue.color = _effectColors[skill.effectType];

            if (skill.minEffectValue != 0) _skillValue.text = _effectNames[skill.effectType] + ": " + skill.minEffectValue + " - " + skill.maxEffectValue;

            if (skill.minDamagePerSecond != 0) _skillValue.text += " |\n"+ _effectNames[skill.damagePerSecondType] + " per second: " + skill.minEffectValue + " - " + skill.maxDamagePerSecond + " (" + skill.damagePerSecondTime + "s)";

            gameObject.transform.GetChild(0).gameObject.SetActive(true);
        }

        public void HideTooltip()
        {
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
            _skillValue.text = "";
        }
    }
}