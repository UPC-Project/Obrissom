using System.Collections.Generic;

// CONSTANT
public static class LevelUpRequirements
{
    public static readonly Dictionary<int, float> LevelRequirements = new Dictionary<int, float>()
    {
        { 1, 100f },
        { 2, 200f },
        { 3, 400f },
        { 4, 1000f },
        { 5, 0f },
    };

    public static readonly int MAX_LEVEL = 5; // constant
}
