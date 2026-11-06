using Obrissom.Enemy;
using Obrissom.Player;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Behaviours/Buff_and_Debuff")]
public class BuffAndDebuff : SkillBehaviour
{
    [Header("Buff (players) - if stat=0 then it's ignored")]
    [SerializeField] private float _buffDuration = 3;
    [SerializeField] private float _bonusHealth = 0;
    [SerializeField] private float _healthRegen = 0;
    [SerializeField] private float _BonusResource = 0;
    [SerializeField] private float _resourceRegen = 0;
    [SerializeField] private float _bonusPhysicalAttack = 0;
    [SerializeField] private float _physicalAttackMultiplier = 0;
    [SerializeField] private float _bonusMagicAttack = 0;
    [SerializeField] private float _magicAttackMultiplier = 0;
    [SerializeField] private float _bonusHeal = 0;
    [SerializeField] private float _bonusSpeed = 0;
    [SerializeField] private float _physicalDefense = 0;
    [SerializeField] private float _magicDefense = 0;
    [SerializeField] private float _criticalChance = 0;
    [SerializeField] private float _criticalDamage = 0;

    [Header("Sphere settings (range)")]
    [SerializeField] private float _sphereRange = 5;

    private Dictionary<Stats, float> _stats;

    [Header("Debuff (enemies)")]
    [SerializeField] private float _debuffDuration = 3;
    // not all debuff are implemented yet
    [SerializeField] private DebuffType _debuff = DebuffType.Root;

    private void OnEnable()
    {
        _stats = new Dictionary<Stats, float>()
            {
                { Stats.BonusHealth, _bonusHealth },
                { Stats.HealthRegen, _healthRegen },
                { Stats.BonusResource, _BonusResource },
                { Stats.ResourceRegen, _resourceRegen },
                { Stats.BonusPhysicalAttack, _bonusPhysicalAttack },
                { Stats.PhysicalAttackMultiplier, _physicalAttackMultiplier},
                { Stats.BonusMagicAttack, _bonusMagicAttack },
                { Stats.MagicAttackMultiplier, _magicAttackMultiplier },
                { Stats.BonusHeal, _bonusHeal},
                { Stats.BonusSpeed, _bonusSpeed },
                { Stats.PhysicalDefense, _physicalDefense },
                { Stats.MagicDefense, _magicDefense },
                { Stats.CriticalChance, _criticalChance },
                { Stats.CriticalDamage, _criticalDamage },
            };
    }
    public override void Execute(GameObject caster, Skill skillData, Vector3 targetPosition)
    {
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, _sphereRange);
        HashSet<PlayerStats> players = new();
        HashSet<EnemyBase> enemies = new();
        foreach (Collider hit in hits)
        {
            if (hit.transform.root.CompareTag("Player"))
            {
                PlayerStats player = hit.transform.root.GetComponent<PlayerStats>();
                if (player == null || players.Contains(player)) continue;
                players.Add(player);
                foreach (var stat in _stats)
                {
                    if (stat.Value > 0) player.AddTempStatRpc(stat.Key, stat.Value,_buffDuration);
                }
            }
            else if (hit.transform.root.CompareTag("Enemy"))
            {
                EnemyBase enemy = hit.transform.root.GetComponent<EnemyBase>();
                if (enemy == null || enemies.Contains(enemy)) continue;
                enemies.Add(enemy);
                enemy.ApplyDebuffRpc(_debuff, _debuffDuration);
            }
        }
    }
}
