using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Obrissom.UI
{
    /// <summary>
    /// Master, Music and Effects volume sliders in a side panel.
    /// Local to each player (no networking) and saved in PlayerPrefs between sessions.
    /// </summary>
    public class Settings : MonoBehaviour
    {
        // Exposed parameter names in MainMixer, also used as PlayerPrefs keys
        private const string MasterParam = "MasterVolume";
        private const string MusicParam  = "MusicVolume";
        private const string SfxParam    = "SFXVolume";

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private InputActionReference _toggleSettingsAction;

        [Header("Sliders (0 to 1)")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("UI Components")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _exitModal;
        [SerializeField] private GameObject _disconnectPanel;
        [SerializeField] private TextMeshProUGUI _warningText;

        // Start, not Awake: AudioMixer.SetFloat is ignored before the mixer is ready
        private void Start()
        {
            Bind(_masterSlider, MasterParam);
            Bind(_musicSlider, MusicParam);
            Bind(_sfxSlider, SfxParam);

            if (_panel != null) _panel.SetActive(false);

            if (LobbyNetworking.Instance != null)
                LobbyNetworking.Instance.OnDisconnecting += ShowDisconnectPanel;
        }

        private void OnEnable()
        {
            _toggleSettingsAction.action.Enable();
            _toggleSettingsAction.action.performed += OnToggleSettings;
        }

        private void OnDisable()
        {
            _toggleSettingsAction.action.performed -= OnToggleSettings;
        }

        private void OnDestroy()
        {
            if (LobbyNetworking.Instance != null)
                LobbyNetworking.Instance.OnDisconnecting -= ShowDisconnectPanel;

            PlayerPrefs.Save();
        }

        public void TogglePanel() 
        {
            _panel.SetActive(!_panel.activeSelf);
        }

        private void OnToggleSettings(InputAction.CallbackContext context)
        {
            if (_exitModal.activeSelf) { TryExitGame(false); return; }
            TogglePanel();
        }

        // AUDIO SETTINGS
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

        // EXIT GAME
        public void TryExitGame(bool tryExit)
        {
            _exitModal.SetActive(tryExit);
            if (tryExit) _panel.SetActive(false);
            if (tryExit && NetworkManager.Singleton.IsHost) _warningText.text = "Are you sure? \n Match will end for everyone";
        }
        /// <summary>
        /// restore initial state, moving player to lobby.
        /// </summary>
        public async void ExitGame()
        {
            _exitModal.SetActive(false);
            await LobbyNetworking.Instance.LeaveLobby();
        }

        private void ShowDisconnectPanel()
        {
            _disconnectPanel.SetActive(true);
        }
    }
}
