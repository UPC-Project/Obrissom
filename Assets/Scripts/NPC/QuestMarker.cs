using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum QuestMarkerType
{
    None,
    Available,      // "!" quest to accept
    InProgress,     // grey "?" quest delivered here, not finished yet
    ReadyToDeliver, // "?" quest to deliver here
}

/// <summary>
/// Shows "!" over an NPC with a quest to accept and "?" where a quest is delivered.
/// Drawn on a screen overlay canvas so the depth of field blur doesn't affect it.
/// Local only: each player sees the markers for their own quests.
/// </summary>
[RequireComponent(typeof(QuestGiver))]
public class QuestMarker : MonoBehaviour
{
    private const float RefreshInterval = 0.25f;
    private const int CanvasSortingOrder = -10; // under the player HUD

    private static Canvas s_canvas;

    [SerializeField] private TMP_FontAsset _font;

    [Header("Size")]
    [Tooltip("Height of the sign in meters, as if it were in the world.")]
    [SerializeField] private float _worldSize = 0.8f;
    [SerializeField] private float _minPixelSize = 22f;
    [SerializeField] private float _maxPixelSize = 90f;

    [Header("Position")]
    [Tooltip("Gap between the top of the NPC (or its interact prompt) and the marker.")]
    [SerializeField] private float _heightAboveHead = 0.4f;
    [SerializeField] private float _maxDistance = 150f;

    [Header("Occlusion")]
    [SerializeField] private bool _hideWhenOccluded = true;
    [Tooltip("Visible objects on these layers hide the marker. Invisible colliders never do.")]
    [SerializeField] private LayerMask _occluders = 137; // Default, Ground, StaticEnvironment

    [Header("Look")]
    [SerializeField] private Color _readyColor = new Color(1f, 0.8f, 0.15f);
    [SerializeField] private Color _inProgressColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private float _bobHeight = 0.12f;
    [SerializeField] private float _bobSpeed = 2.5f;

    private readonly RaycastHit[] _hits = new RaycastHit[16];

    private QuestGiver _questGiver;
    private PlayerQuestTracker _localPlayer;
    private TextMeshProUGUI _label;
    private Vector3 _anchor;
    private QuestMarkerType _type;
    private bool _occluded;
    private float _nextRefresh;

    private void Awake()
    {
        _questGiver = GetComponent<QuestGiver>();
        _anchor = transform.position + Vector3.up * (GetTopHeight() + _heightAboveHead);
        CreateLabel();
    }

    private void OnDestroy()
    {
        if (_label != null) Destroy(_label.gameObject);
    }

    private void OnDisable()
    {
        if (_label != null) _label.enabled = false;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;

        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + RefreshInterval;
            PlayerQuestTracker player = GetLocalPlayer();
            _type = player != null ? _questGiver.GetMarker(player) : QuestMarkerType.None;
            _occluded = _hideWhenOccluded && cam != null && IsOccluded(cam.transform.position);
        }

        Vector3 world = _anchor + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
        Vector3 screen = cam != null ? cam.WorldToScreenPoint(world) : Vector3.back;

        // screen.z is the distance in front of the camera
        bool visible = _type != QuestMarkerType.None && !_occluded && screen.z > 0f && screen.z <= _maxDistance;
        _label.enabled = visible;
        if (!visible) return;

        _label.text = _type == QuestMarkerType.Available ? "!" : "?";
        _label.color = _type == QuestMarkerType.InProgress ? _inProgressColor : _readyColor;
        _label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);

        // Same size a world object would have, clamped so far markers stay readable
        float pixels = _worldSize * Screen.height / (2f * screen.z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
        _label.fontSize = Mathf.Clamp(pixels, _minPixelSize, _maxPixelSize);
    }

    private void CreateLabel()
    {
        var go = new GameObject($"Quest Marker ({name})", typeof(RectTransform));
        go.transform.SetParent(GetCanvas().transform, false);

        _label = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) _label.font = _font;
        _label.alignment = TextAlignmentOptions.Center;
        _label.overflowMode = TextOverflowModes.Overflow;
        _label.raycastTarget = false;
        _label.rectTransform.sizeDelta = new Vector2(100f, 100f);
        _label.enabled = false;
    }

    // One overlay canvas for all markers, it goes away with the scene
    private static Canvas GetCanvas()
    {
        if (s_canvas != null) return s_canvas;

        var go = new GameObject("Quest Markers Canvas");
        s_canvas = go.AddComponent<Canvas>();
        s_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        s_canvas.sortingOrder = CanvasSortingOrder;
        return s_canvas;
    }

    // True if something visible stands between the camera and the marker
    private bool IsOccluded(Vector3 cameraPosition)
    {
        Vector3 toMarker = _anchor - cameraPosition;
        float distance = toMarker.magnitude;
        if (distance < 0.01f) return false;

        int count = Physics.RaycastNonAlloc(cameraPosition, toMarker / distance, _hits, distance, _occluders, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i].collider;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit is TerrainCollider) return true;

            // Invisible walls have no renderer, they don't hide the marker
            Renderer rend = hit.GetComponentInChildren<Renderer>();
            if (rend == null) rend = hit.GetComponentInParent<Renderer>();
            if (rend != null && rend.enabled) return true;
        }
        return false;
    }

    // Height of the highest renderer or canvas (interact prompt) above the NPC pivot
    private float GetTopHeight()
    {
        float top = float.MinValue;

        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            if (rend is ParticleSystemRenderer) continue;
            top = Mathf.Max(top, rend.bounds.max.y);
        }

        var corners = new Vector3[4];
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
        {
            ((RectTransform)canvas.transform).GetWorldCorners(corners);
            foreach (Vector3 corner in corners) top = Mathf.Max(top, corner.y);
        }

        return top == float.MinValue ? 2f : top - transform.position.y;
    }

    private PlayerQuestTracker GetLocalPlayer()
    {
        if (_localPlayer != null) return _localPlayer;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return null;

        _localPlayer = manager.LocalClient.PlayerObject.GetComponent<PlayerQuestTracker>();
        return _localPlayer;
    }
}
