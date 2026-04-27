using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    #region Class variables
    [SerializeField, Min(0)] private float _health;
    [Min(0)] public float maxHealth;
    [SerializeField, Min(0)] private float resource; // Mana / Stamina / Fury, etc
    [Min(0)] public float maxResource;
    [SerializeField] private bool _isUsingSkill;
    public List<Skill> skills; // available skills
   
    [Header("Stats")]
    public float attackBoost = 1f;
    public float damageReduction = 1f;
    public float speedBoost = 1f;
    [Min(0)]public float criticChance = 0f;
    [SerializeField, Range(0,100)] public int criticPercentage = 0;
    #endregion

    // TODO: save information:
    // for example current health, level, bufs, skills available, etc
    // Should be saved in a file. --> Make save system

    private void Start()
    {
        // Load player data from file
    }

    public void ApplyStat(StatType stat, float value)
    {
        switch (stat)
        {
            case StatType.MoreHealth:
                maxHealth += value;
                break;
            case StatType.MoreResource:
                maxResource += value;
                break;
            case StatType.AttackBoost:
                attackBoost += value;
                break;
            case StatType.MitigateDamage:
                damageReduction += value;
                break;
            case StatType.SpeedBoost:
                speedBoost += value;
                break; // TODO
            case StatType.CriticPercentage:
                criticChance += value;
                break;
        }
    }

    public void UnlockSkill(Skill skill)
    {
        // TODO
    }

    public void Attack(Skill skill)
    {
            if (_isUsingSkill) return;
    
            _isUsingSkill = true;
            Debug.Log($"Player is attacking with {skill.skillName}");
    }
}
