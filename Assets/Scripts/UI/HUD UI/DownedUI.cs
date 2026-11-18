using Obrissom.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Obrissom.UI
{
    /// Local player feedback for the downed state, driven by PlayerRespawn on the owner:
    /// grey screen + time left, the "respawn at bonfire" / "use magic stone" buttons,
    /// and the "Hold F" prompt next to a downed teammate.
    /// Keep this GameObject active (it hides its own panels).
    public class DownedUI : MonoBehaviour
    {
        public static DownedUI Instance { get; private set; }

        [Header("Downed")]
        [SerializeField] private GameObject _downedPanel;
        [SerializeField] private TMP_Text _downedText;
        [Tooltip("A teammate can still revive you. {0} = seconds left")]
        [SerializeField] private string _timerFormat = "You are down\nRevive time left: {0}";
        [Tooltip("Nobody left to revive you (solo or whole party down). {0} = seconds left")]
        [SerializeField] private string _noTeammateFormat = "You died\nReturning to the bonfire in {0}";
        [SerializeField] private string _beingRevivedText = "A teammate is reviving you...";
        [SerializeField] private string _deadText = "You died";

        [Header("Choices while downed")]
        [SerializeField] private Button _bonfireButton;
        [SerializeField] private Button _stoneButton;
        [SerializeField] private TMP_Text _stoneButtonText;
        [Tooltip("{0} = magic stones in the inventory")]
        [SerializeField] private string _stoneButtonFormat = "Use Magic Stone ({0})";

        [Header("Revive prompt")]
        [SerializeField] private GameObject _revivePrompt;
        [SerializeField] private TMP_Text _revivePromptText;
        [SerializeField] private string _promptText = "Hold F to revive";
        [SerializeField] private string _revivingText = "Reviving...";

        [Header("Grey screen")]
        [Tooltip("Saturation while downed. -100 = black and white.")]
        [SerializeField, Range(-100f, 0f)] private float _saturation = -100f;
        [Tooltip("Seconds to fade the grey in and out.")]
        [SerializeField] private float _fadeDuration = 0.5f;

        private PlayerRespawn _localPlayer;
        private Volume _greyVolume;
        private float _targetWeight;
        private string _currentDownedText;
        private string _currentPromptText;
        private int _currentStoneCount = -1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Wired here so the buttons need no OnClick setup in the inspector
            if (_bonfireButton != null) _bonfireButton.onClick.AddListener(OnBonfireClicked);
            if (_stoneButton != null) _stoneButton.onClick.AddListener(OnStoneClicked);

            CreateGreyVolume();
            SetActive(_downedPanel, false);
            SetActive(_revivePrompt, false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_greyVolume != null) Destroy(_greyVolume.sharedProfile);
        }

        private void Update()
        {
            if (_greyVolume == null) return;

            float step = _fadeDuration > 0f ? Time.deltaTime / _fadeDuration : 1f;
            _greyVolume.weight = Mathf.MoveTowards(_greyVolume.weight, _targetWeight, step);
        }

        /// The local player's PlayerRespawn, which the buttons talk to.
        public void Bind(PlayerRespawn localPlayer)
        {
            _localPlayer = localPlayer;
        }

        public void ShowDowned(float timeLeft, bool beingRevived, bool respawning, bool teammateAlive, int stoneCount)
        {
            _targetWeight = 1f;
            SetActive(_downedPanel, true);

            string text;
            if (respawning) text = _deadText;
            else if (beingRevived) text = _beingRevivedText;
            else text = string.Format(teammateAlive ? _timerFormat : _noTeammateFormat, Mathf.CeilToInt(timeLeft));

            // Only touch TMP when the text changes (it's called every frame)
            if (text != _currentDownedText && _downedText != null)
            {
                _currentDownedText = text;
                _downedText.text = text;
            }

            // Once respawning there's nothing left to choose
            if (_bonfireButton != null) SetActive(_bonfireButton.gameObject, !respawning);
            if (_stoneButton != null)
            {
                SetActive(_stoneButton.gameObject, !respawning);
                _stoneButton.interactable = stoneCount > 0;
            }

            if (stoneCount != _currentStoneCount && _stoneButtonText != null)
            {
                _currentStoneCount = stoneCount;
                _stoneButtonText.text = string.Format(_stoneButtonFormat, stoneCount);
            }
        }

        public void HideDowned()
        {
            _targetWeight = 0f;
            _currentDownedText = null;
            SetActive(_downedPanel, false);
        }

        public void ShowRevivePrompt(bool reviving)
        {
            SetActive(_revivePrompt, true);

            string text = reviving ? _revivingText : _promptText;
            if (text != _currentPromptText && _revivePromptText != null)
            {
                _currentPromptText = text;
                _revivePromptText.text = text;
            }
        }

        public void HideRevivePrompt()
        {
            _currentPromptText = null;
            SetActive(_revivePrompt, false);
        }

        private void OnBonfireClicked()
        {
            if (_localPlayer != null) _localPlayer.RequestRespawnAtBonfire();
        }

        private void OnStoneClicked()
        {
            if (_localPlayer != null) _localPlayer.RequestStoneRevive();
        }

        // Global post-processing volume built at runtime: no scene setup needed.
        // On the Default layer, which is the one the player camera's Volume Mask reads.
        private void CreateGreyVolume()
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.saturation.Override(_saturation);

            GameObject volumeObject = new GameObject("Downed Grey Volume") { layer = 0 };
            volumeObject.transform.SetParent(transform, false);

            _greyVolume = volumeObject.AddComponent<Volume>();
            _greyVolume.isGlobal = true;
            _greyVolume.priority = 100f;
            _greyVolume.weight = 0f;
            _greyVolume.sharedProfile = profile;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
