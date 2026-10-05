using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Server only. Tracks which enemies fight each player and limits attacks at the same time.
    /// </summary>
    public static class EnemyCombatCoordinator
    {
        private struct AttackSlot
        {
            public EnemyBase Holder;
            public float ExpiresAt;
        }

        private class TargetEntry
        {
            public readonly List<EnemyBase> Engagers = new List<EnemyBase>();
            public readonly List<AttackSlot> AttackSlots = new List<AttackSlot>();
            public float LastAttackStartTime = float.NegativeInfinity;
        }

        private static readonly Dictionary<Transform, TargetEntry> s_entries = new Dictionary<Transform, TargetEntry>();
        private static readonly List<Transform> s_staleTargets = new List<Transform>();
        private static readonly List<EnemyBase> s_empty = new List<EnemyBase>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_entries.Clear();

        // Engagement

        public static void Engage(Transform target, EnemyBase enemy)
        {
            if (target == null || enemy == null) return;
            TargetEntry entry = GetOrCreate(target);
            if (!entry.Engagers.Contains(enemy)) entry.Engagers.Add(enemy);
        }

        public static void Disengage(Transform target, EnemyBase enemy)
        {
            if (target == null || !s_entries.TryGetValue(target, out TargetEntry entry)) return;

            entry.Engagers.Remove(enemy);
            RemoveSlotsOf(entry, enemy);

            if (entry.Engagers.Count == 0 && entry.AttackSlots.Count == 0) s_entries.Remove(target);
        }

        public static int GetEngagedCount(Transform target, EnemyBase exclude = null)
        {
            if (target == null || !s_entries.TryGetValue(target, out TargetEntry entry)) return 0;

            Prune(entry);
            int count = entry.Engagers.Count;
            if (exclude != null && entry.Engagers.Contains(exclude)) count--;
            return count;
        }

        // Arrival order (0 = first), -1 if not engaged
        public static int GetEngageRank(Transform target, EnemyBase enemy)
        {
            if (target == null || !s_entries.TryGetValue(target, out TargetEntry entry)) return -1;
            Prune(entry);
            return entry.Engagers.IndexOf(enemy);
        }

        public static IReadOnlyList<EnemyBase> GetEngagers(Transform target)
        {
            if (target == null || !s_entries.TryGetValue(target, out TargetEntry entry)) return s_empty;
            Prune(entry);
            return entry.Engagers;
        }

        // Attack slots

        // Slots expire after maxHoldTime, so a lost release never blocks a player
        public static bool TryAcquireAttackSlot(Transform target, EnemyBase enemy, int maxSimultaneous,
                                                float minTimeBetweenAttacks, float maxHoldTime)
        {
            if (target == null || enemy == null) return false;

            TargetEntry entry = GetOrCreate(target);
            Prune(entry);

            for (int i = 0; i < entry.AttackSlots.Count; i++)
                if (entry.AttackSlots[i].Holder == enemy) return true;

            if (entry.AttackSlots.Count >= maxSimultaneous) return false;
            if (Time.time - entry.LastAttackStartTime < minTimeBetweenAttacks) return false;

            entry.AttackSlots.Add(new AttackSlot { Holder = enemy, ExpiresAt = Time.time + maxHoldTime });
            entry.LastAttackStartTime = Time.time;
            return true;
        }

        public static void ReleaseAttackSlot(Transform target, EnemyBase enemy)
        {
            if (target == null || !s_entries.TryGetValue(target, out TargetEntry entry)) return;
            RemoveSlotsOf(entry, enemy);
        }

        // Call on death or despawn
        public static void RemoveEverywhere(EnemyBase enemy)
        {
            s_staleTargets.Clear();
            foreach (KeyValuePair<Transform, TargetEntry> pair in s_entries)
            {
                pair.Value.Engagers.Remove(enemy);
                RemoveSlotsOf(pair.Value, enemy);
                if (pair.Key == null || (pair.Value.Engagers.Count == 0 && pair.Value.AttackSlots.Count == 0))
                    s_staleTargets.Add(pair.Key);
            }

            foreach (Transform stale in s_staleTargets) s_entries.Remove(stale);
        }

        // Helpers

        private static TargetEntry GetOrCreate(Transform target)
        {
            if (!s_entries.TryGetValue(target, out TargetEntry entry))
            {
                entry = new TargetEntry();
                s_entries.Add(target, entry);
            }
            return entry;
        }

        private static void Prune(TargetEntry entry)
        {
            entry.Engagers.RemoveAll(e => e == null || e.IsDead);
            entry.AttackSlots.RemoveAll(s => s.Holder == null || s.Holder.IsDead || Time.time > s.ExpiresAt);
        }

        private static void RemoveSlotsOf(TargetEntry entry, EnemyBase enemy)
        {
            for (int i = entry.AttackSlots.Count - 1; i >= 0; i--)
                if (entry.AttackSlots[i].Holder == enemy) entry.AttackSlots.RemoveAt(i);
        }
    }
}
