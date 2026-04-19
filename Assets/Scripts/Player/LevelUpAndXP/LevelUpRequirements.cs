using System.Collections.Generic;

// CONSTANT
public class LevelUpRequirements
{
    // TODO: This class will also need to check another requirements: the craftable objects or other objects needed
    // For now, is only checking the XP needed

    public Dictionary<int, float> LevelRequirements = new Dictionary<int, float>()
    {
        { 1, 100f },
        { 2, 200f },
        { 3, 300f },
        { 4, 400f },
        { 5, 0f },
    };

    public int MAX_LEVEL = 5; // constant
}
