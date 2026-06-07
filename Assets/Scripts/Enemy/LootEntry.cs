using System;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Represents a possible item drop with a weight-based probability.
    /// Higher weight = more likely to drop relative to other entries.
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        public Item item;
        [Min(1)] public int quantity = 1;
        [Min(0f)] public float weight = 1f;
    }
}