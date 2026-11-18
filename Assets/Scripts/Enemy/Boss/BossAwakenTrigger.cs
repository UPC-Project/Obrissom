using Obrissom.Player;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Trigger volume inside the church. The first player who walks in wakes the boss.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BossAwakenTrigger : MonoBehaviour
    {
        [SerializeField] private MantisBoss _boss;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Every copy of every player crosses this trigger: only your own player sends the request
            PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
            if (player == null || !player.IsOwner) return;
            if (_boss == null || !_boss.IsSpawned || _boss.IsAwake || _boss.IsDead) return;

            _boss.RequestAwakenRpc();
        }
    }
}
