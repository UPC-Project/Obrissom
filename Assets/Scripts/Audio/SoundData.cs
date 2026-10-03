using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Obrissom.Audio
{
    /// <summary>
    /// One sound and its variants. Each play picks a random clip and a random pitch in range.
    /// Lives inside a soundLibrary list.
    /// </summary>
    [Serializable]
    public class SoundData
    {
        [Tooltip("Identifier used from code: AudioManager.Instance.PlaySound(AudioID.X).")]
        public AudioID id;

        [Tooltip("Variants. One is picked at random every time the sound plays.")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Header("Pitch (1 - 1 = no variation)")]
        [Range(0.1f, 3f)] public float pitchMin = 1f;
        [Range(0.1f, 3f)] public float pitchMax = 1f;

        [Header("3D (only when played with a position)")]
        [Tooltip("Distance at which the sound starts to fade.")]
        [Min(0f)] public float minDistance = 1f;

        [Tooltip("Distance at which the sound is no longer audible.")]
        [Min(0f)] public float maxDistance = 25f;

        public bool HasClips => clips != null && clips.Length > 0;

        /// <summary>
        /// Returns a random clip from the list, or null if the list is empty.
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (!HasClips) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float GetRandomPitch() => Random.Range(pitchMin, pitchMax);

        /// <summary>
        /// Fixes invalid values typed in the inspector. Called from SoundLibrary.OnValidate.
        /// </summary>
        public void Validate()
        {
            // A freshly added list element can come with zeroed fields
            if (id == AudioID.None && !HasClips && pitchMin <= 0f && pitchMax <= 0f)
            {
                volume = 1f;
                pitchMin = 1f;
                pitchMax = 1f;
                minDistance = 1f;
                maxDistance = 25f;
            }

            if (pitchMin > pitchMax) pitchMax = pitchMin;
            if (minDistance > maxDistance) maxDistance = minDistance;
        }
    }
}
