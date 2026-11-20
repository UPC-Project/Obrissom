using System;
using System.Collections.Generic;
using Obrissom.Player;
using UnityEngine;

namespace Obrissom.UI
{
    public class StatsMenu : MenuPanel
    {
        [SerializeField] private StatRowUI _statRowPrefab;
        [SerializeField] private Transform _statsListContainer;

        private static readonly (Stats stat, string label, Func<PlayerStats, string> format)[] s_statDefinitions =
        {
        (Stats.HealthRegen,              "Health Regen",           s => $"{s.healthRegen:0.##}/s"),
        (Stats.ResourceRegen,            "Resource Regen",         s => $"{s.resourceRegen:0.##}/s"),
        (Stats.BonusSpeed,               "Bonus Speed",            s => $"{s.bonusSpeed:0.##}"),
        (Stats.BonusHeal,                "Bonus Heal",             s => $"{s.bonusHeal:0.##}"),
        (Stats.BonusPhysicalAttack,      "Physical Attack",        s => $"{s.bonusPhysicalAttack:0.##}"),
        (Stats.PhysicalAttackMultiplier, "Physical Attack Mult",   s => $"x{s.physicalAttackMultiplier:0.##}"),
        (Stats.BonusMagicAttack,         "Magic Attack",           s => $"{s.bonusMagicAttack:0.##}"),
        (Stats.MagicAttackMultiplier,    "Magic Attack Mult",      s => $"x{s.magicAttackMultiplier:0.##}"),
        (Stats.PhysicalDefense,          "Physical Defense",       s => $"{s.physicalDefense * 100f:0.#}%"),
        (Stats.MagicDefense,             "Magic Defense",          s => $"{s.magicDefense * 100f:0.#}%"),
        (Stats.CriticalChance,           "Critical Chance",        s => $"{s.criticalChance * 100f:0.#}%"),
        (Stats.CriticalDamage,           "Critical Damage",        s => $"{s.criticalDamage * 100f:0.#}%"),
    };

        private PlayerStats _playerStats;
        private readonly Dictionary<Stats, StatRowUI> _rows = new Dictionary<Stats, StatRowUI>();

        public void BindPlayerStats(PlayerStats playerStats)
        {
            _playerStats = playerStats;

            if (_rows.Count == 0)
            {
                foreach (var def in s_statDefinitions)
                    _rows[def.stat] = Instantiate(_statRowPrefab, _statsListContainer);
            }

            RefreshUI();
        }

        public override void SetMenuState(bool state)
        {
            base.SetMenuState(state);
            if (state) RefreshUI();
        }

        private void RefreshUI()
        {
            if (_playerStats == null) return;

            foreach (var def in s_statDefinitions)
                _rows[def.stat].Setup(def.label, def.format(_playerStats));
        }
    }
}
