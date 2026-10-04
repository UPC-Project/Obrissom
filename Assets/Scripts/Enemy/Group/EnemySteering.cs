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

        /// <summary>
        /// Nudges a desired bearing (degrees around the target) away from the bearings of the other engagers,
        /// so enemies fighting the same player naturally surround it instead of stacking on one side.
        /// </summary>
        public static float SpreadBearing(Transform target, EnemyBase self, float desiredBearing,
                                          float minSeparation, IReadOnlyList<EnemyBase> engagers)
        {
            int others = 0;
            for (int i = 0; i < engagers.Count; i++)
                if (engagers[i] != null && engagers[i] != self && !engagers[i].IsDead) others++;
            if (others == 0) return desiredBearing;

            // With many engagers the ideal spacing shrinks so they still fit around the circle
            float separation = Mathf.Min(minSeparation, 360f / (others + 1) * 0.9f);
            Vector3 targetPosition = target.position;
            float push = 0f;

            for (int i = 0; i < engagers.Count; i++)
            {
                EnemyBase other = engagers[i];
                if (other == null || other == self || other.IsDead) continue;

                Vector3 offset = other.transform.position - targetPosition;
                float otherBearing = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(otherBearing, desiredBearing);
                float absDelta = Mathf.Abs(delta);
                if (absDelta >= separation) continue;

                float side = absDelta > 0.01f
                    ? Mathf.Sign(delta)
                    : (self.GetInstanceID() > other.GetInstanceID() ? 1f : -1f);
                push += side * (separation - absDelta) * 0.5f;
            }

            return desiredBearing + Mathf.Clamp(push, -separation, separation);
        }
    }
}
