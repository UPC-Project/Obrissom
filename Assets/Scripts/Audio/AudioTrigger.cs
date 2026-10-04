using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Audio
{
    /// <summary>
    /// Asks the audioManager to play a sound when something enters or exits this trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AudioTrigger : MonoBehaviour
    {
        [SerializeField] private AudioID _sound;

        [Header("When")]
        [SerializeField] private bool _playOnEnter = true;
        [SerializeField] private bool _playOnExit;

        [Tooltip("Plays only the first time (enter or exit). Counted per client.")]
        [SerializeField] private bool _playOnce;

        [Header("Who")]
        [Tooltip("Layers that can activate the trigger.")]
        [SerializeField] private LayerMask _detectLayers = ~0;

        [Tooltip("Only the player controlled on this machine activates it. Use for feedback meant for that player only.")]
        [SerializeField] private bool _onlyLocalPlayer;

        [Header("How")]
        [Tooltip("3D: plays at this object's position. 2D: same volume everywhere.")]
        [SerializeField] private bool _play3D = true;

        private bool _hasPlayed;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            // A kinematic body guarantees trigger events even when the entering object
            // is moved by NetworkTransform (remote players) instead of physics
            if (GetComponentInParent<Rigidbody>() == null)
                gameObject.AddComponent<Rigidbody>().isKinematic = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_playOnEnter && IsValid(other)) Play();
        }

        private void OnTriggerExit(Collider other)
        {
            if (_playOnExit && IsValid(other)) Play();
        }

        private bool IsValid(Collider other)
        {
            if ((_detectLayers.value & (1 << other.gameObject.layer)) == 0) return false;

            if (_onlyLocalPlayer)
            {
                NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();
                if (networkObject == null || !networkObject.IsLocalPlayer) return false;
            }

            return true;
        }

        private void Play()
        {
            if (_playOnce && _hasPlayed) return;
            if (AudioManager.Instance == null) return;

            _hasPlayed = true;

            if (_play3D)
                AudioManager.Instance.PlaySound(_sound, transform.position);
            else
                AudioManager.Instance.PlaySound(_sound);
        }
    }
}
