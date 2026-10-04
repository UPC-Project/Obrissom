using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Obrissom.Audio
{
    /// <summary>
    /// Master, Music and Effects volume sliders in a side panel.
    /// Local to each player (no networking) and saved in PlayerPrefs between sessions.
    /// </summary>
    public class VolumeSettings : MonoBehaviour
    {
        // Exposed parameter names in MainMixer, also used as PlayerPrefs keys
        private const string MasterParam = "MasterVolume";
        private const string MusicParam  = "MusicVolume";
        private const string SfxParam    = "SFXVolume";

        [SerializeField] private AudioMixer _mixer;

        [Header("Sliders (0 to 1)")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Side Panel")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _toggleButton;

        // Start, not Awake: AudioMixer.SetFloat is ignored before the mixer is ready
        private void Start()
        {
            Bind(_masterSlider, MasterParam);
            Bind(_musicSlider, MusicParam);
            Bind(_sfxSlider, SfxParam);

            if (_toggleButton != null) _toggleButton.onClick.AddListener(TogglePanel);
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }

        public void TogglePanel()
        {
            if (_panel != null) _panel.SetActive(!_panel.activeSelf);
        }

        private void Bind(Slider slider, string param)
        {
            float value = PlayerPrefs.GetFloat(param, 1f);
            Apply(param, value);

            if (slider == null) return;

            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(newValue =>
            {
                Apply(param, newValue);
                PlayerPrefs.SetFloat(param, newValue);
            });
        }

        // Sliders are linear (0–1), the mixer works in decibels (-80 to 0)
        private void Apply(string param, float value)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(param, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
        }
    }
}
