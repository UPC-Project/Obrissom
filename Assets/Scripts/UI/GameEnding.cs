using System.Collections;
using Obrissom.Audio;
using Obrissom.Player;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Obrissom.UI
{
    /// <summary>
    /// End of the game. When the Mantis falls it plays the closing cinematic (a Timeline, like the lobby one)
    /// and then lets each player leave the world or keep playing. Runs locally on every player.
    /// </summary>
    public class GameEnding : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        [Tooltip("Cinematic cameras, turned on only while the ending plays.")]
        [SerializeField] private GameObject _cameras;
        [Tooltip("Seconds after the boss dies, so the victory banner is seen first.")]
        [SerializeField] private float _startDelay = 4f;

        [Header("Fade between the church and the village")]
        [Tooltip("Timeline time where the church shot ends and the village shot starts.")]
        [SerializeField] private float _cutTime = 6f;
        [SerializeField] private float _fadeToBlack = 0.6f;
        [SerializeField] private float _blackHold = 0.4f;
        [SerializeField] private float _fadeFromBlack = 1.5f;

        [Header("Panel")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private string _title = "The village is free";
        [SerializeField] private string _subtitle = "Thank you for playing";
        [SerializeField] private string _leaveText = "Leave the world";
        [SerializeField] private string _stayText = "Keep playing";
        [SerializeField] private string _hostWarning = "As the host, leaving ends the match for everyone";

        private bool _played;
        private GameObject _panel;
        private GameObject _fadeCanvas;
        private Image _fade;
        private HudToggle _hud;
        private bool _hudWasVisible;
        private Behaviour[] _playerControls;
        private bool[] _controlsWereEnabled;

        // Called on every player when the Mantis dies (MantisBoss.ShowVictoryRpc)
        public void Play()
        {
            if (_played || _director == null) return;
            _played = true;
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            yield return new WaitForSeconds(_startDelay);

            SetPlayerControl(false);
            SetHudVisible(false);
            if (_cameras != null) _cameras.SetActive(true);
            BindLocalCamera();

            _director.time = 0;
            _director.Play();
            CreateFade();

            // The timeline holds its last shot until the player picks an option
            float elapsed = 0f;
            while (elapsed < _director.duration)
            {
                elapsed += Time.deltaTime;
                _fade.color = new Color(0f, 0f, 0f, FadeAlpha((float)_director.time));
                yield return null;
            }
            _fade.color = Color.clear;
            ShowPanel();
        }

        // Black screen between the shots: fades in before the cut, holds, then fades away on the village
        private float FadeAlpha(float time)
        {
            float start = _cutTime - _fadeToBlack;
            if (time < start) return 0f;
            if (time < _cutTime) return (time - start) / Mathf.Max(_fadeToBlack, 0.01f);
            if (time < _cutTime + _blackHold) return 1f;
            return 1f - Mathf.Clamp01((time - _cutTime - _blackHold) / Mathf.Max(_fadeFromBlack, 0.01f));
        }

        // The cinematic track drives this player's own camera brain (each player prefab has one)
        private void BindLocalCamera()
        {
            CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (brain == null) brain = FindAnyObjectByType<CinemachineBrain>();
            if (brain == null || _director.playableAsset == null) return;

            foreach (PlayableBinding output in _director.playableAsset.outputs)
            {
                if (output.outputTargetType == typeof(CinemachineBrain))
                    _director.SetGenericBinding(output.sourceObject, brain);
            }
        }

        // OPTIONS

        private void LeaveWorld()
        {
            CloseCinematic();
            if (LobbyNetworking.Instance != null) _ = LobbyNetworking.Instance.LeaveLobby();
            else Debug.LogWarning("[GameEnding] No LobbyNetworking in the game, can't leave the world.");
        }

        private void KeepPlaying()
        {
            CloseCinematic();
            SetPlayerControl(true);
            SetHudVisible(true);
        }

        private void CloseCinematic()
        {
            if (_panel != null) Destroy(_panel);
            if (_fadeCanvas != null) Destroy(_fadeCanvas);
            _director.Stop();
            if (_cameras != null) _cameras.SetActive(false);
        }

        // PLAYER

        // Turns off the same parts the skills turn off while casting, and puts back only what was on
        private void SetPlayerControl(bool enabled)
        {
            if (!enabled)
            {
                GameObject player = GetLocalPlayer();
                if (player == null) return;

                _playerControls = new Behaviour[]
                {
                    player.GetComponent<PlayerLocomotionInput>(),
                    player.GetComponent<PlayerCombatInput>(),
                    player.GetComponent<PlayerController>(),
                };
                _controlsWereEnabled = new bool[_playerControls.Length];
            }
            if (_playerControls == null) return;

            for (int i = 0; i < _playerControls.Length; i++)
            {
                if (_playerControls[i] == null) continue;
                if (!enabled) _controlsWereEnabled[i] = _playerControls[i].enabled;
                _playerControls[i].enabled = enabled && _controlsWereEnabled[i];
            }
        }

        private void SetHudVisible(bool visible)
        {
            if (!visible)
            {
                _hud = FindAnyObjectByType<HudToggle>();
                if (_hud == null) return;
                _hudWasVisible = _hud.Visible;
            }
            if (_hud != null) _hud.Visible = visible && _hudWasVisible;
        }

        private static GameObject GetLocalPlayer()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return null;
            return manager.LocalClient.PlayerObject.gameObject;
        }

        // PANEL (made in code, like the quest waypoint)

        private void ShowPanel()
        {
            _panel = new GameObject("Game Ending Panel", typeof(RectTransform));
            Canvas canvas = _panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = _panel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _panel.AddComponent<GraphicRaycaster>();

            CreateFullScreen("Shade", _panel.transform, new Color(0f, 0f, 0f, 0.55f));

            CreateText("Title", _title, 72f, new Vector2(0f, 170f), new Vector2(1400f, 100f));
            CreateText("Subtitle", _subtitle, 36f, new Vector2(0f, 90f), new Vector2(1400f, 60f));
            CreateButton("Leave Button", _leaveText, new Vector2(-190f, -40f), LeaveWorld);
            CreateButton("Stay Button", _stayText, new Vector2(190f, -40f), KeepPlaying);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                CreateText("Host Warning", _hostWarning, 24f, new Vector2(0f, -130f), new Vector2(1400f, 40f));
        }

        // Own canvas under the final panel, so the panel always draws on top
        private void CreateFade()
        {
            _fadeCanvas = new GameObject("Game Ending Fade", typeof(RectTransform));
            Canvas canvas = _fadeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;
            _fade = CreateFullScreen("Black", _fadeCanvas.transform, Color.clear);
        }

        private Image CreateFullScreen(string name, Transform parent, Color color)
        {
            Image image = CreateRect(name, parent, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private TextMeshProUGUI CreateText(string name, string text, float size, Vector2 position, Vector2 boxSize, Transform parent = null)
        {
            RectTransform rect = CreateRect(name, parent != null ? parent : _panel.transform, position, boxSize);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private void CreateButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateRect(name, _panel.transform, position, new Vector2(340f, 80f));
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.1f, 0.08f, 0.9f);

            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.8f, 0.15f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(onClick);
            rect.gameObject.AddComponent<UIButtonSound>();

            TextMeshProUGUI label = CreateText("Label", text, 32f, Vector2.zero, rect.sizeDelta, rect);
            label.color = new Color(1f, 0.85f, 0.4f);
        }
    }
}
