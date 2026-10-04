using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Obrissom.Audio
{
    /// <summary>
    /// A group of sounds (player, enemy, environment, ui). One asset per group.
    /// The audioManager reads every assigned library and indexes the sounds by AudioID.
    /// </summary>
    [CreateAssetMenu(fileName = "New SoundLibrary", menuName = "Obrissom/Audio/SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Tooltip("Optional. Leave empty if no AudioMixer is used.")]
        [SerializeField] private AudioMixerGroup _output;

        [SerializeField] private List<SoundData> _sounds = new List<SoundData>();

        public AudioMixerGroup Output => _output;

        public IReadOnlyList<SoundData> Sounds => _sounds;

        private void OnValidate()
        {
            foreach (SoundData sound in _sounds)
                sound?.Validate();
        }
    }
}
