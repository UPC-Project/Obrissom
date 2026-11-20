using System;
using UnityEngine;

namespace Obrissom.Enemy
{
    public enum RootShape
    {
        UnderPlayers,
        RandomCircles,
        LinesAcross,
        Fan,
        ParallelLines,
        Rings
    }

    [Serializable]
    public class RootWave
    {
        public RootShape shape;

        [Tooltip("Seconds after the attack starts.")]
        [Min(0f)] public float delay;

        [Tooltip("Root size. 1 = normal.")]
        [Min(0.1f)] public float size = 1f;

        [Tooltip("Circles: how many. Lines: how many lines. Rings: how many rings.")]
        [Min(1)] public int count = 1;

        [Tooltip("Lines Across / Parallel Lines / Rings: meters between lines or rings. Fan: total angle in degrees.")]
        [Min(0f)] public float spread = 5f;

        [Tooltip("Lines and rings: meters between roots.")]
        [Min(0.5f)] public float rootSpacing = 2f;

        [Tooltip("Rings: radius of the first ring around the boss.")]
        [Min(0f)] public float radius = 2.5f;

        [Tooltip("Lines: max length in meters (walls cut it shorter).")]
        [Min(0f)] public float length = 24f;

        [Tooltip("Lines: seconds between one root and the next, so they come out one after another (0 = all at once). " +
                 "Circles: random extra delay up to this value. Rings: seconds between one ring and the next.")]
        [Min(0f)] public float stagger;

        public RootWave() { }

        public RootWave(RootShape shape, float delay, float size, int count, float spread, float rootSpacing, float length, float stagger, float radius = 2.5f)
        {
            this.radius = radius;
            this.shape = shape;
            this.delay = delay;
            this.size = size;
            this.count = count;
            this.spread = spread;
            this.rootSpacing = rootSpacing;
            this.length = length;
            this.stagger = stagger;
        }
    }

    [Serializable]
    public class RootPattern
    {
        public string name;

        [Tooltip("Animator trigger played when the boss casts this attack.")]
        public string animatorTrigger = "Roots";

        [Tooltip("Cast when players stay next to the boss too long.")]
        public bool antiMelee;

        [Tooltip("Chance compared to the other attacks.")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("Seconds between casts: the boss casts it on its own timer. 0 = picked at random with the others.")]
        [Min(0f)] public float interval;

        public RootWave[] waves;

        public RootPattern() { }

        public RootPattern(string name, bool antiMelee, params RootWave[] waves)
        {
            this.name = name;
            this.antiMelee = antiMelee;
            this.waves = waves;
        }

        public RootPattern(string name, float interval, params RootWave[] waves)
        {
            this.name = name;
            this.interval = interval;
            this.waves = waves;
        }
    }
}
