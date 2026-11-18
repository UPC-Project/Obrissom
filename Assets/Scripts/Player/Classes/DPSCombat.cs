using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Bridge between networking and skill behaviours.
/// Specific for DPS class.
/// </summary>
namespace Obrissom.Player
{
    public class DPSCombat : NetworkBehaviour
    {
        [Header("Magic Ring Visual")]
        [Tooltip("Prefab puramente visual del anillo mágico (sin NetworkObject).")]
        [SerializeField] private GameObject _magicRingPrefab;

        private GameObject _magicRingInstance;
        private Coroutine _magicRingHideRoutine;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ChannelMagicProjectileServerRpc(Vector3 spawnPosition, Vector3 directionToTarget, int minDamage, int maxDamage, float speed, float lifeTime)
        {
            ChannelMagicProjectile.ExecuteOnServer(gameObject, spawnPosition, directionToTarget, minDamage, maxDamage, speed, lifeTime);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void PhysicInstantFrontServerRpc(int minDamage, int maxDamage, float range, float angle, float originOffset, EffectType effect, int minDamagePerSecond, int maxDamagePerSecond, float damagePerSecondTime, EffectType damagePerSecondType)
        {
            // Manually had to pass all skill data needed, since RPC functions only acept serializable types (maybe could use INetworkSerializable)
            InstantFront.ExecuteOnServer(gameObject, minDamage, maxDamage, range, angle, originOffset, effect, minDamagePerSecond, maxDamagePerSecond, damagePerSecondTime, damagePerSecondType);
        }

        // MAGIC RING VISUAL

        /// <summary>
        /// Called by MagicDamageRing on the casting client (owner).
        /// </summary>
        public void ShowMagicRing(Vector3 position, Quaternion rotation, float duration)
        {
            if (!IsOwner) return;

            PlayMagicRingLocal(position, rotation, duration);
            ShowMagicRingServerRpc(position, rotation, duration);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ShowMagicRingServerRpc(Vector3 position, Quaternion rotation, float duration)
        {
            ShowMagicRingClientRpc(position, rotation, duration);
        }

        [Rpc(SendTo.NotOwner)]
        public void ShowMagicRingClientRpc(Vector3 position, Quaternion rotation, float duration)
        {
            PlayMagicRingLocal(position, rotation, duration);
        }

        private void PlayMagicRingLocal(Vector3 position, Quaternion rotation, float duration)
        {
            if (_magicRingPrefab == null)
            {
                Debug.LogWarning("DPSCombat: no hay _magicRingPrefab asignado.");
                return;
            }

            if (_magicRingInstance == null)
                _magicRingInstance = Instantiate(_magicRingPrefab, position, rotation);

            _magicRingInstance.SetActive(false);
            _magicRingInstance.transform.SetPositionAndRotation(position, rotation);
            _magicRingInstance.SetActive(true);

            if (_magicRingHideRoutine != null)
                StopCoroutine(_magicRingHideRoutine);

            _magicRingHideRoutine = StartCoroutine(HideMagicRingAfter(duration));
        }

        private IEnumerator HideMagicRingAfter(float duration)
        {
            yield return new WaitForSeconds(duration);

            if (_magicRingInstance != null)
                _magicRingInstance.SetActive(false);

            _magicRingHideRoutine = null;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (_magicRingInstance != null)
                Destroy(_magicRingInstance);
        }
    }
}