using System.Collections.Generic;
using Obrissom.Player;
using UnityEngine;

namespace Obrissom.Enemy
{
    [RequireComponent(typeof(Collider))]
    public class BossAwakenTrigger : MonoBehaviour
    {
        private const float RequestInterval = 0.5f;

        [SerializeField] private MantisBoss _boss;

        private static readonly List<BossAwakenTrigger> s_arenas = new List<BossAwakenTrigger>();

        private Collider _volume;
        private float _nextRequestTime;

        // True if the position is inside any boss room
        public static bool IsInsideAnyArena(Vector3 position)
        {
            foreach (BossAwakenTrigger arena in s_arenas)
            {
                if (arena.Contains(position)) return true;
            }
            return false;
        }

        private void Awake()
        {
            _volume = GetComponent<Collider>();
            _volume.isTrigger = true;
            if (_boss != null) _boss.SetArena(this);
        }

        private void OnEnable() => s_arenas.Add(this);

        private void OnDisable() => s_arenas.Remove(this);

        public bool Contains(Vector3 position) =>
            (_volume.ClosestPoint(position) - position).sqrMagnitude < 0.0001f;

        public Vector3 RandomPoint()
        {
            if (_volume is BoxCollider box)
            {
                Vector3 local = box.center + Vector3.Scale(box.size, new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f));
                return box.transform.TransformPoint(local);
            }

            Bounds bounds = _volume.bounds;
            return new Vector3(Random.Range(bounds.min.x, bounds.max.x), bounds.center.y, Random.Range(bounds.min.z, bounds.max.z));
        }

        private void OnTriggerStay(Collider other)
        {
            if (_boss == null || !_boss.IsSpawned || _boss.IsAwake || _boss.IsDead) return;
            if (Time.time < _nextRequestTime) return;

            PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
            if (player == null || !player.IsOwner || !player.IsTargetable) return;

            _nextRequestTime = Time.time + RequestInterval;
            _boss.RequestAwakenRpc();
        }
    }
}
