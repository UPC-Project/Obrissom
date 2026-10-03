using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Obrissom.Audio
{
    /// <summary>
    /// Local entry point for playing sounds: AudioManager.Instance.PlaySound(AudioID.X).
    /// One per client, never networked. Networked events reach it through RPCs on gameplay objects.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Tooltip("Every SoundLibrary the game uses (Player, Enemy, Environment, UI...).")]
        [SerializeField] private SoundLibrary[] _libraries;

        private readonly Dictionary<AudioID, (SoundData data, AudioMixerGroup output)> _sounds =
            new Dictionary<AudioID, (SoundData, AudioMixerGroup)>();

        [Tooltip("Logs the clip and pitch chosen on every play.")]
        [SerializeField] private bool _logPlays;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildSoundIndex();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Plays a 2D sound (UI, menus, local feedback).
        /// </summary>
        public void PlaySound(AudioID id)
        {
            if (!TryGetSound(id, out SoundData data, out AudioMixerGroup output)) return;

            AudioClip clip = data.GetRandomClip();
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Sound {id} has an empty clip slot.");
                return;
            }

            AudioSource source = CreateSource();
            source.clip = clip;
            source.volume = data.volume;
            source.pitch = data.GetRandomPitch();
            source.outputAudioMixerGroup = output;
            source.Play();

            if (_logPlays)
                Debug.Log($"[AudioManager] {id}: {clip.name} @ pitch {source.pitch:F2}");

            // Temporary until pooling: the source lives just long enough to finish the clip
            Destroy(source.gameObject, clip.length / source.pitch + 0.1f);
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("AudioSource");
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private bool TryGetSound(AudioID id, out SoundData data, out AudioMixerGroup output)
        {
            data = null;
            output = null;

            if (!_sounds.TryGetValue(id, out var entry))
            {
                Debug.LogWarning($"[AudioManager] No sound registered for {id}.");
                return false;
            }

            if (!entry.data.HasClips)
            {
                Debug.LogWarning($"[AudioManager] Sound {id} has no clips assigned.");
                return false;
            }

            data = entry.data;
            output = entry.output;
            return true;
        }

        private void BuildSoundIndex()
        {
            _sounds.Clear();
            if (_libraries == null) return;

            foreach (SoundLibrary library in _libraries)
            {
                if (library == null) continue;

                foreach (SoundData sound in library.Sounds)
                {
                    if (sound == null || sound.id == AudioID.None) continue;

                    if (_sounds.ContainsKey(sound.id))
                    {
                        Debug.LogWarning($"[AudioManager] Duplicate AudioID {sound.id} in {library.name}. Ignored.");
                        continue;
                    }

                    _sounds.Add(sound.id, (sound, library.Output));
                }
            }
        }
    }
}
