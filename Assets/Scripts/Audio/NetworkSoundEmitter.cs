using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Audio
{
   
    public class NetworkSoundEmitter : NetworkBehaviour
    {
        /// <summary>
        /// Server only. Plays the sound on every client (host included, once) at this object's position.
        /// </summary>
        public void PlayForEveryone(AudioID id) => PlayForEveryone(id, transform.position);

        /// <summary>
        /// Server only. Plays the sound on every client (host included, once) at the given position.
        /// </summary>
        public void PlayForEveryone(AudioID id, Vector3 position)
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[NetworkSoundEmitter] PlayForEveryone({id}) must be called on the server.", this);
                return;
            }

            PlaySoundRpc(id, position);
        }

        /// <summary>
        /// Owner only. Plays the sound locally right away and asks the server to play it for everyone else.
        /// </summary>
        public void PlayFromOwner(AudioID id) => PlayFromOwner(id, transform.position);

        /// <summary>
        /// Owner only. Plays the sound locally right away and asks the server to play it for everyone else.
        /// </summary>
        public void PlayFromOwner(AudioID id, Vector3 position)
        {
            if (!IsOwner)
            {
                Debug.LogWarning($"[NetworkSoundEmitter] PlayFromOwner({id}) must be called by the owner.", this);
                return;
            }

            PlayLocal(id, position);
            RelayToNonOwnersRpc(id, position);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlaySoundRpc(AudioID id, Vector3 position) => PlayLocal(id, position);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RelayToNonOwnersRpc(AudioID id, Vector3 position) => PlaySoundForNonOwnersRpc(id, position);

        // The owner already played it in PlayFromOwner
        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        private void PlaySoundForNonOwnersRpc(AudioID id, Vector3 position) => PlayLocal(id, position);

        private void PlayLocal(AudioID id, Vector3 position)
        {
            // A dedicated server has no listener, nothing to play
            if (IsServer && !IsClient) return;
            if (AudioManager.Instance == null) return;

            AudioManager.Instance.PlaySound(id, position);
        }
    }
}
