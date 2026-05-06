using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerStats : MonoBehaviour
    {
        #region Class variables

        [Tooltip("Total health"), Min(0)]
        public float maxHealth = 20f;
        [Tooltip("Health regeneration per second"), Min(0)]
        public float healthRegen = 1f;
        [Tooltip("Total resource (mana/stamina/fury)"), Min(0)]
        public float maxResource = 10f;
        [Tooltip("Resource regeneration per second"), Min(0)]
        public float resourceRegen = 1f;
        [Tooltip("Damage added to base Attack (only)")]
        public float bonusPhysicalAttack = 0f;
        [Tooltip("Multiplies total Attack Damage"), Min(1)]
        public float physicalAttackMultiplier = 1f;
        [Tooltip("Damage added to base Magic Attack (only for magic skills)")]
        public float bonusMagicAttack = 0f;
        [Tooltip("Multiplies total Magic Damage"), Min(1)]
        public float magicAttackMultiplier = 1f;
        [Tooltip("Heal added to base Heal (only for healing skills)")]
        public float bonusHeal = 0f;
        [Tooltip("Increases base speed")]
        public float bonusSpeed = 0f;
        [Tooltip("Reduces incoming physical damage"), Range(0f, 0.99f)]
        public float physicalDefense = 0f;
        [Tooltip("Reduces incoming magical damage"), Range(0f, 0.99f)]
        public float magicDefense = 0f;
        [Tooltip("Percentage chance to deal a critical hit"), Range(0f, 1f)]
        public float criticalChance = 0.1f;
        [Tooltip("Multiplier for critical hit damage"), Min(1.1f)]
        public float criticalDamage = 1.1f;

        private Dictionary<Stats, Action<float>> _statApplicatorsAdd;
        private Dictionary<Stats, Action<float>> _statApplicatorsRemove;

        #endregion
        private void Awake()
        {
            _statApplicatorsAdd = new Dictionary<Stats, Action<float>>()
            {
                { Stats.BonusHealth,      value => maxHealth += value },
                { Stats.HealthRegen,      value => healthRegen += value },
                { Stats.BonusResource,    value => maxResource += value },
                { Stats.ResourceRegen,    value => resourceRegen += value },
                { Stats.BonusPhysicalAttack,      value => bonusPhysicalAttack += value },
                { Stats.PhysicalAttackMultiplier, value => physicalAttackMultiplier += value },
                { Stats.BonusMagicAttack, value => bonusMagicAttack += value },
                { Stats.MagicAttackMultiplier,  value => magicAttackMultiplier += value },
                { Stats.BonusHeal,        value => bonusHeal += value },
                { Stats.BonusSpeed,       value => bonusSpeed += value },
                { Stats.PhysicalDefense,  value => physicalDefense += value },
                { Stats.MagicDefense,   value => magicDefense += value },
                { Stats.CriticalChance,   value => criticalChance += value },
                { Stats.CriticalDamage,   value => criticalDamage += value },
            };

            _statApplicatorsRemove = new Dictionary<Stats, Action<float>>()
            {
                { Stats.BonusHealth,      value => maxHealth -= value },
                { Stats.HealthRegen,      value => healthRegen -= value },
                { Stats.BonusResource,    value => maxResource -= value },
                { Stats.ResourceRegen,    value => resourceRegen -= value },
                { Stats.BonusPhysicalAttack,      value => bonusPhysicalAttack -= value },
                { Stats.PhysicalAttackMultiplier, value => physicalAttackMultiplier -= value },
                { Stats.BonusMagicAttack, value => bonusMagicAttack -= value },
                { Stats.MagicAttackMultiplier,  value => magicAttackMultiplier -= value },
                { Stats.BonusHeal,        value => bonusHeal -= value },
                { Stats.BonusSpeed,       value => bonusSpeed -= value },
                { Stats.PhysicalDefense,  value => physicalDefense -= value },
                { Stats.MagicDefense,   value => magicDefense -= value },
                { Stats.CriticalChance,   value => criticalChance -= value },
                { Stats.CriticalDamage,   value => criticalDamage -= value },
            };
        }

        public void AddStat(Stats stat, float value)
        {
            if (_statApplicatorsAdd.TryGetValue(stat, out Action<float> apply))
                apply(value);
            else
                Debug.LogWarning($"Applicator not found for stat: {stat}");
        }

        public void RemoveStat(Stats stat, float value)
        {
            if (_statApplicatorsRemove.TryGetValue(stat, out Action<float> apply))
                apply(value);
            else
                Debug.LogWarning($"Applicator not found for stat: {stat}");
        }
    }
}