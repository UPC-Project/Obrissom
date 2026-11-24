using System.Collections.Generic;
using Obrissom.Player;
using TMPro;
using UnityEngine;

namespace Obrissom.UI
{

    public class PlayerNameplates : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _fontSize = 24f;

        [Tooltip("Gap between the top of the player and the name.")]
        [SerializeField] private float _heightAboveHead = 0.3f;
        [SerializeField] private float _maxDistance = 80f;
        [SerializeField] private bool _showOwnName = false;

        private class Nameplate
        {
            public TextMeshProUGUI Label;
            public CharacterController Body;
        }

        private readonly Dictionary<PlayerCombat, Nameplate> _nameplates = new();
        private readonly List<PlayerCombat> _gone = new();
        private RectTransform _root;
        private float _nextRefresh;

        private void Awake()
        {
            var root = new GameObject("Player Names", typeof(RectTransform));
            _root = (RectTransform)root.transform;
            _root.SetParent(transform, false);
            _root.SetAsFirstSibling(); // menus and popups draw on top
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                RefreshPlayers();
            }

            Camera cam = Camera.main;
            foreach (var pair in _nameplates)
            {
                TextMeshProUGUI label = pair.Value.Label;
                if (pair.Key == null || cam == null)
                {
                    label.enabled = false;
                    continue;
                }

                // screen.z is the distance in front of the camera
                Vector3 screen = cam.WorldToScreenPoint(GetHeadPosition(pair.Key, pair.Value.Body));
                bool visible = screen.z > 0f && screen.z <= _maxDistance;
                label.enabled = visible;
                if (visible) label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            }
        }

        // Adds players that joined, removes the ones that left and keeps the names up to date
        private void RefreshPlayers()
        {
            foreach (PlayerCombat player in FindObjectsByType<PlayerCombat>(FindObjectsSortMode.None))
            {
                if (!player.IsSpawned || (player.IsOwner && !_showOwnName) || _nameplates.ContainsKey(player)) continue;
                _nameplates[player] = new Nameplate { Label = CreateLabel(), Body = player.GetComponent<CharacterController>() };
            }

            _gone.Clear();
            foreach (var pair in _nameplates)
            {
                if (pair.Key == null || !pair.Key.IsSpawned) _gone.Add(pair.Key);
                else pair.Value.Label.text = pair.Key._playerName.Value.ToString();
            }

            foreach (PlayerCombat player in _gone)
            {
                Destroy(_nameplates[player].Label.gameObject);
                _nameplates.Remove(player);
            }
        }

        // Top of the character controller, works even when it is turned off on other players
        private Vector3 GetHeadPosition(PlayerCombat player, CharacterController body)
        {
            Transform t = player.transform;
            float top = body != null
                ? t.TransformPoint(body.center + Vector3.up * body.height * 0.5f).y
                : t.position.y + 2f;
            return new Vector3(t.position.x, top + _heightAboveHead, t.position.z);
        }

        private TextMeshProUGUI CreateLabel()
        {
            var go = new GameObject("Player Name", typeof(RectTransform));
            go.transform.SetParent(_root, false);

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.fontSize = _fontSize;
            label.color = _color;
            label.alignment = TextAlignmentOptions.Bottom;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 220);
            label.rectTransform.sizeDelta = new Vector2(300f, _fontSize * 1.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.enabled = false;
            return label;
        }
    }
}
