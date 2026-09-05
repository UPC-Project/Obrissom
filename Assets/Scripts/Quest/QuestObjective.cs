using System;
using UnityEngine;

/// Each objective has a type that determines which target fields are relevant
[Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;

    [Header("Kill — enemies to defeat")]
    public EnemyTarget[] enemyTargets;
        
    [Header("Collect — items to gather")]
    public ItemTarget[] itemTargets;

    [Header("Talk — NPC to interact with (NPC ID)")]
    public string targetNPC;
}
