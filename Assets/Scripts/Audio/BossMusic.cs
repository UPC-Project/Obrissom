using Obrissom.Enemy;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Audio
{
    /// <summary>
    /// Crossfades the scene music into the boss theme while the local player is in the arena and the boss is fighting.
    /// Goes on the scene Music object, next to its AudioSource. Local only, nothing is networked.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BossMusic : MonoBehaviour
    {
        private const float BossSearchInterval = 1f;

        [SerializeField] private AudioClip _bossTheme;

        [Range(0f, 1f)] [SerializeField] private float _bossVolume = 1f;

        [Tooltip("Seconds to crossfade between the scene music and the boss theme.")]
        [Min(0.01f)] [SerializeField] private float _fadeTime = 2f;

        private AudioSource _sceneMusic;
        private AudioSource _bossMusic;
        private float _sceneVolume;

        // 0 = scene music, 1 = boss theme
        private float _blend;

        private MantisBoss _boss;
        private float _nextBossSearch;

        private void Awake()
        {
            _sceneMusic = GetComponent<AudioSource>();
            _sceneVolume = _sceneMusic.volume;

            // Same mixer group as the scene music, so the music volume setting applies
            _bossMusic = gameObject.AddComponent<AudioSource>();
            _bossMusic.clip = _bossTheme;
            _bossMusic.outputAudioMixerGroup = _sceneMusic.outputAudioMixerGroup;
            _bossMusic.loop = true;
            _bossMusic.playOnAwake = false;
            _bossMusic.spatialBlend = 0f;
            _bossMusic.volume = 0f;
        }

        private void Update()
        {
            if (_bossTheme == null) return;

            float target = IsLocalPlayerInBossFight() ? 1f : 0f;
            if (_blend == target) return;

            _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime / _fadeTime);

            // The boss theme starts from the beginning on every fight
            if (_blend > 0f && !_bossMusic.isPlaying) _bossMusic.Play();
            if (_blend <= 0f) _bossMusic.Stop();

            // The scene music pauses during the fight and continues where it was after
            if (_blend >= 1f) _sceneMusic.Pause();
            else if (!_sceneMusic.isPlaying) _sceneMusic.UnPause();

            _sceneMusic.volume = _sceneVolume * (1f - _blend);
            _bossMusic.volume = _bossVolume * _blend;
        }

        private bool IsLocalPlayerInBossFight()
        {
            if (_boss == null)
            {
                // The boss can spawn after this scene loads
                if (Time.unscaledTime < _nextBossSearch) return false;
                _nextBossSearch = Time.unscaledTime + BossSearchInterval;

                _boss = FindFirstObjectByType<MantisBoss>();
                if (_boss == null) return false;
            }

            if (!_boss.IsSpawned || !_boss.IsFighting) return false;

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null) return false;

            NetworkObject player = manager.LocalClient.PlayerObject;
            return player != null && BossAwakenTrigger.IsInsideAnyArena(player.transform.position);
        }
    }
}
