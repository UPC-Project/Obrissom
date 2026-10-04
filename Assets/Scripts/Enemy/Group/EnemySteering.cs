using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Server-only steering helpers shared by group-aware enemies.
    /// NavMeshAgent handles pathfinding and local avoidance; these helpers decide WHERE each agent should go
    /// so a group spreads out instead of queuing on the same point.
    /// </summary>
    public static class EnemySteering
    {
        private static readonly List<EnemyBase> s_agents = new List<EnemyBase>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_agents.Clear();

        public static void Register(EnemyBase enemy)
        {
            if (!s_agents.Contains(enemy)) s_agents.Add(enemy);
        }

        public static void Unregister(EnemyBase enemy) => s_agents.Remove(enemy);

        /// <summary>
        /// Push away from registered enemies closer than radius. Stronger the closer they are, zero at radius.
        /// Linear scan: cheap for dozens of enemies at the AI eval rate. Swap for a spatial hash if counts reach hundreds.
        /// </summary>
        public static Vector3 ComputeSeparation(EnemyBase self, float radius)
        {
            if (radius <= 0f) return Vector3.zero;

            Vector3 position = self.transform.position;
            float sqrRadius = radius * radius;
            Vector3 push = Vector3.zero;

            for (int i = 0; i < s_agents.Count; i++)
            {
                EnemyBase other = s_agents[i];
                if (other == null || other == self || other.IsDead) continue;

                Vector3 away = position - other.transform.position;
                away.y = 0f;
                float sqrDistance = away.sqrMagnitude;
                if (sqrDistance >= sqrRadius) continue;

                if (sqrDistance < 0.0001f)
                {
                    // Exactly overlapping: stable, opposite directions per pair so they split instead of jittering
                    int a = self.GetInstanceID();
                    int b = other.GetInstanceID();
                    int hash = (Mathf.Min(a, b) * 73856093) ^ (Mathf.Max(a, b) * 19349663);
                    float angle = (hash & 0x7fffffff) % 360;
                    away = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (a < b ? 1f : -1f);
                    sqrDistance = 0.0001f;
                }

                float distance = Mathf.Sqrt(sqrDistance);
                push += away / distance * (1f - distance / radius);
            }

            return push;
        }
    }
}
