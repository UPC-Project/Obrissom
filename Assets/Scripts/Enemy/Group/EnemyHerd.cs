using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Shared roaming area for a group of enemies. Each member wanders on its own (own destinations, speed and pauses);
    /// the herd only hands out destinations that are spread apart, so two members never head to the same spot.
    /// Server only: members are networked through their own NetworkTransform, the herd itself is not synced.
    /// A solo enemy is just a herd of one.
    /// </summary>
    public class EnemyHerd : MonoBehaviour
    {
        private const int DestinationAttempts = 12;

        [Header("Roaming Area")]
        [Tooltip("Radius around this transform where random destinations are picked when there are no waypoints.")]
        [SerializeField, Min(0f)] private float _roamRadius = 15f;

        [Tooltip("Optional. If set, members wander around these points instead of the whole radius.")]
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("How far around a waypoint a member may pick its destination.")]
        [SerializeField, Min(0f)] private float _waypointSpread = 4f;

        [Tooltip("Random pause (seconds) when a member reaches its destination.")]
        [SerializeField] private Vector2 _pauseAtDestination = new Vector2(2f, 5f);

        [Header("Alert")]
        [Tooltip("Members within this distance of the one that spotted a player are alerted.")]
        [SerializeField, Min(0f)] private float _alertRadius = 18f;

        [Tooltip("Random reaction delay (seconds) for alerted members.")]
        [SerializeField] private Vector2 _alertDelay = new Vector2(0.15f, 0.6f);

        [Tooltip("Extra reaction delay per meter of distance to the member that spotted the player.")]
        [SerializeField, Min(0f)] private float _alertDelayPerMeter = 0.04f;

        public Vector2 PauseAtDestination => _pauseAtDestination;

        private readonly List<IHerdMember> _members = new List<IHerdMember>();
        private readonly Dictionary<IHerdMember, Vector3> _claimedDestinations = new Dictionary<IHerdMember, Vector3>();
        private bool _destroyWhenEmpty;

        /// <summary>Setup for herds created at runtime (spawners, solo enemies).</summary>
        public void Configure(float roamRadius, Vector2 pauseAtDestination, GameObject[] waypoints, bool destroyWhenEmpty)
        {
            _roamRadius = roamRadius;
            _pauseAtDestination = pauseAtDestination;
            _destroyWhenEmpty = destroyWhenEmpty;
            SetWaypoints(waypoints);
        }

        public void SetWaypoints(GameObject[] waypoints)
        {
            var valid = new List<Transform>();
            if (waypoints != null)
                foreach (GameObject point in waypoints)
                    if (point != null) valid.Add(point.transform);

            _waypoints = valid.ToArray();
        }

        // Membership

        public void AddMember(IHerdMember member)
        {
            if (member == null || _members.Contains(member)) return;

            _members.Add(member);
            member.SetHerd(this);
        }

        public void RemoveMember(IHerdMember member)
        {
            if (!_members.Remove(member)) return;

            _claimedDestinations.Remove(member);
            if (_members.Count == 0 && _destroyWhenEmpty) Destroy(gameObject);
        }

        // Alert

        /// <summary>A member spotted a target: wake up nearby free members with staggered delays.</summary>
        public void RaiseAlert(IHerdMember source, Transform target)
        {
            if (target == null) return;

            Vector3 sourcePosition = source.transform.position;
            float sqrRadius = _alertRadius * _alertRadius;

            foreach (IHerdMember member in _members)
            {
                if (member == source || member.IsEngaged) continue;

                float sqrDistance = (member.transform.position - sourcePosition).sqrMagnitude;
                if (sqrDistance > sqrRadius) continue;

                float delay = Random.Range(_alertDelay.x, _alertDelay.y) + Mathf.Sqrt(sqrDistance) * _alertDelayPerMeter;
                member.OnHerdAlert(target, delay);
            }
        }

        // Destinations

        /// <summary>
        /// Picks a random NavMesh point in the area that keeps at least the member's spacing from the destinations
        /// claimed by the others and from where the others are standing. Falls back to the best candidate found.
        /// </summary>
        public bool TryGetDestination(IHerdMember member, out Vector3 destination)
        {
            float spacing = member.HerdSpacing;
            destination = member.transform.position;
            float bestClearance = -1f;
            bool found = false;

            for (int attempt = 0; attempt < DestinationAttempts; attempt++)
            {
                if (!NavMesh.SamplePosition(GetCandidate(), out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;

                float clearance = GetClearance(member, hit.position);
                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    destination = hit.position;
                    found = true;
                }

                if (clearance >= spacing) break;
            }

            if (found) _claimedDestinations[member] = destination;
            return found;
        }

        /// <summary>The member left its destination behind (combat, death...). Frees the spot for others.</summary>
        public void ReleaseDestination(IHerdMember member) => _claimedDestinations.Remove(member);

        private Vector3 GetCandidate()
        {
            if (_waypoints != null && _waypoints.Length > 0)
            {
                Transform waypoint = _waypoints[Random.Range(0, _waypoints.Length)];
                if (waypoint != null) return RandomAround(waypoint.position, _waypointSpread);
            }

            return RandomAround(transform.position, _roamRadius);
        }

        // Distance to the closest spot already taken by another member (its claimed destination or its position)
        private float GetClearance(IHerdMember member, Vector3 candidate)
        {
            float clearance = float.MaxValue;

            foreach (IHerdMember other in _members)
            {
                if (other == member) continue;

                clearance = Mathf.Min(clearance, FlatDistance(candidate, other.transform.position));
                if (_claimedDestinations.TryGetValue(other, out Vector3 claimed))
                    clearance = Mathf.Min(clearance, FlatDistance(candidate, claimed));
            }

            return clearance;
        }

        private static Vector3 RandomAround(Vector3 center, float radius)
        {
            Vector2 random = Random.insideUnitCircle * radius;
            return center + new Vector3(random.x, 0f, random.y);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
            if (_waypoints != null && _waypoints.Length > 0)
            {
                foreach (Transform waypoint in _waypoints)
                    if (waypoint != null) Gizmos.DrawWireSphere(waypoint.position, _waypointSpread);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, _roamRadius);
            }

            if (!Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            foreach (KeyValuePair<IHerdMember, Vector3> claim in _claimedDestinations)
            {
                Gizmos.DrawWireSphere(claim.Value, 0.25f);
                Gizmos.DrawLine(claim.Key.transform.position, claim.Value);
            }
        }
    }
}
