using Obrissom.Player;
using UnityEngine;

namespace Obrissom.Enemy
{
    [RequireComponent(typeof(Collider))]
    public class BossAwakenTrigger : MonoBehaviour
    {
        private const float RequestInterval = 0.5f;

        [SerializeField] private MantisBoss _boss;

        private Collider _volume;
        private float _nextRequestTime;

        private void Awake()
        {
            _volume = GetComponent<Collider>();
            _volume.isTrigger = true;
            if (_boss != null) _boss.SetArena(this);
        }

        public bool Contains(Vector3 position) =>
            (_volume.ClosestPoint(position) - position).sqrMagnitude < 0.0001f;

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
