using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    #region Class variables
    [SerializeField, Min(0)] private float _health;
    [SerializeField] private bool _isUsingSkill;
    public List<Skill> skills; // available skills
   
    [Header("Bufs")]
    public float attackBoost = 1f;
    public float mitigateDamage = 1f;
    public float speedBoost = 1f;
    [SerializeField, Range(0,100)] public int criticPercentage = 0;
    #endregion

    // TODO: save information:
    // for example current health, level, bufs, skills available, etc
    // Should be saved in a file. --> Make save system

    private void Start()
    {
        // Load player data from file
    }

    public void Attack(Skill skill)
    {
            if (_isUsingSkill) return;
    
            _isUsingSkill = true;
            Debug.Log($"Player is attacking with {skill.skillName}");
    }
}
