using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public float xp { get; private set; }
    public float xpNeeded; // XP needed for next level
    public int currentLevel = 1;
    //[SerializeField] private LevelUpRequirements requirements;

    private void Start()
    {
        // TODO: save information
        // Load player data from file
        //xpNeeded = requirements.LevelRequirements[currentLevel];
    }
    public void GainXP(float amount)
    {
        //if (currentLevel >= requirements.maxlevel) return; 
        if (xp + amount >= xpNeeded)
        {
            float rest = (xp + amount) - xpNeeded;
            xp = rest;
            LevelUp();
        }
        else
        {
            xp += amount;
        }
    }

    [ContextMenu("Level Up")]
    private void LevelUp()
    {
        currentLevel++;
       //xpNeeded = requirements.LevelRequirements[currentLevel];

        // TODO: here will recieve the rewards and apply them
    }
}
