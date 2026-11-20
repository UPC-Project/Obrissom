using System.Collections;
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

        // LOOP: a sound that follows this object until it is stopped (channel skills)

        private const float LoopFadeOutTime = 0.15f;

        private AudioSource _loopSource;
        private Coroutine _loopFade;

        /// <summary>
        /// Owner only. Loops the sound on this object for everyone until StopLoopFromOwner.
        /// </summary>
        public void PlayLoopFromOwner(AudioID id)
        {
            if (!IsOwner)
            {
                Debug.LogWarning($"[NetworkSoundEmitter] PlayLoopFromOwner({id}) must be called by the owner.", this);
                return;
            }

            PlayLoopLocal(id);
            RelayLoopRpc(id);
        }

        /// <summary>
        /// Owner only. Stops the loop started with PlayLoopFromOwner, for everyone.
        /// </summary>
        public void StopLoopFromOwner()
        {
            if (!IsOwner) return;

            StopLoopLocal();
            RelayLoopRpc(AudioID.None);
        }

        public override void OnNetworkDespawn()
        {
            if (_loopSource != null) _loopSource.Stop();
            base.OnNetworkDespawn();
        }

        // None means stop
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RelayLoopRpc(AudioID id) => LoopForNonOwnersRpc(id);

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        private void LoopForNonOwnersRpc(AudioID id)
        {
            if (id == AudioID.None) StopLoopLocal();
            else PlayLoopLocal(id);
        }

        private void PlayLoopLocal(AudioID id)
        {
            if (IsServer && !IsClient) return;
            if (AudioManager.Instance == null) return;

            if (_loopSource == null)
            {
                var go = new GameObject("Loop Sound");
                go.transform.SetParent(transform, false);
                _loopSource = go.AddComponent<AudioSource>();
                _loopSource.playOnAwake = false;
            }

            if (_loopFade != null)
            {
                StopCoroutine(_loopFade);
                _loopFade = null;
            }

            AudioManager.Instance.PlayOn(_loopSource, id, loop: true);
        }

        private void StopLoopLocal()
        {
            if (_loopSource == null || !_loopSource.isPlaying) return;

            if (_loopFade != null) StopCoroutine(_loopFade);
            _loopFade = StartCoroutine(FadeOutLoop());
        }

        // Short fade so the cut doesn't click
        private IEnumerator FadeOutLoop()
        {
            float startVolume = _loopSource.volume;
            for (float t = 0f; t < LoopFadeOutTime; t += Time.unscaledDeltaTime)
            {
                _loopSource.volume = Mathf.Lerp(startVolume, 0f, t / LoopFadeOutTime);
                yield return null;
            }

            _loopSource.Stop();
            _loopFade = null;
        }
    }
}
