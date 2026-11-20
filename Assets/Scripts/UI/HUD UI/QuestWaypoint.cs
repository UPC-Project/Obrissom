using Obrissom.Enemy;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Obrissom.UI
{
    /// <summary>
    /// HUD arrow that points to where the current quest goes next, visible from anywhere.
    /// On screen it floats over the target; off screen it sticks to the border and points to it.
    /// Goes on the player HUD canvas. Local only.
    /// </summary>
    public class QuestWaypoint : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Color _color = new Color(1f, 0.8f, 0.15f);
        [SerializeField] private float _arrowSize = 34f;
        [SerializeField] private float _fontSize = 26f;

        [Tooltip("Distance from the screen border when the target is off screen (HUD units).")]
        [SerializeField] private float _screenMargin = 70f;

        [Tooltip("The arrow points this high above the target.")]
        [SerializeField] private float _targetHeight = 2.8f;

        [Tooltip("Hidden when the target is on screen and closer than this, its own marker is enough.")]
        [SerializeField] private float _arriveDistance = 12f;

        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _arrow;
        private TextMeshProUGUI _distanceText;
        private int _shownDistance = -1;

        private PlayerQuestTracker _player;
        private Transform _target;
        private float _nextRefresh;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            BuildUI();
            _root.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                _player = GetLocalPlayer();
                _target = _player != null ? FindTarget(_player) : null;
            }

            Camera cam = Camera.main;
            if (_target == null || _player == null || cam == null)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            float distance = Vector3.Distance(_player.transform.position, _target.position);
            Vector3 screen = cam.WorldToScreenPoint(_target.position + Vector3.up * _targetHeight);
            float scale = _canvas.scaleFactor;
            float margin = _screenMargin * scale;

            bool behind = screen.z < 0f;
            bool onScreen = !behind && screen.x > margin && screen.x < Screen.width - margin
                && screen.y > margin && screen.y < Screen.height - margin;

            if (onScreen && distance < _arriveDistance)
            {
                _root.gameObject.SetActive(false);
                return;
            }

            _root.gameObject.SetActive(true);
            SetDistance(distance);

            if (onScreen)
            {
                // Over the target, arrow pointing down at it
                _root.position = new Vector3(screen.x, screen.y, 0f);
                _arrow.localRotation = Quaternion.Euler(0f, 0f, 180f);
                _distanceText.rectTransform.anchoredPosition = new Vector2(0f, _arrowSize * 0.5f + _fontSize * 0.6f);
                return;
            }

            // On the border, arrow pointing out toward the target
            Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
            Vector2 dir = (Vector2)screen - center;
            if (behind) dir = -dir;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
            dir.Normalize();

            Vector2 half = center - new Vector2(margin, margin);
            float toBorder = Mathf.Min(half.x / Mathf.Max(Mathf.Abs(dir.x), 0.0001f), half.y / Mathf.Max(Mathf.Abs(dir.y), 0.0001f));
            Vector2 position = center + dir * toBorder;

            _root.position = new Vector3(position.x, position.y, 0f);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            _distanceText.rectTransform.anchoredPosition = -dir * (_arrowSize * 0.5f + _fontSize);
        }

        private void SetDistance(float distance)
        {
            int meters = Mathf.RoundToInt(distance);
            if (meters == _shownDistance) return;

            _shownDistance = meters;
            _distanceText.text = $"{meters} m";
        }

        // TARGET

        // Where the player should go now: deliver, talk, hunt, collect, or the next quest giver
        private Transform FindTarget(PlayerQuestTracker player)
        {
            Vector3 from = player.transform.position;
            QuestInstance quest = PickQuest(player);

            if (quest == null) return FindNearestQuestGiver(player, from);

            QuestTemplate template = quest.template;
            QuestObjective objective = template.objective;

            if (quest.status == QuestStatus.ReadyToDeliver || objective == null) return FindNpc(template.questReceiver);

            switch (objective.type)
            {
                case QuestObjectiveType.Talk:
                    return FindNpc(quest.IsTalkCompleted() ? template.questReceiver : objective.targetNPC);
                case QuestObjectiveType.Kill:
                    return FindNearestEnemy(quest, from);
                case QuestObjectiveType.Collect:
                    return FindNearestCollectable(objective, from) ?? FindNpc(template.questReceiver);
                case QuestObjectiveType.Rest:
                    return FindNearestBonfire(from);
                default:
                    return null;
            }
        }

        // A quest to deliver first, otherwise the first one in progress
        private static QuestInstance PickQuest(PlayerQuestTracker player)
        {
            QuestInstance inProgress = null;
            foreach (QuestInstance quest in player.ActiveQuests)
            {
                if (quest.template == null) continue;
                if (quest.status == QuestStatus.ReadyToDeliver) return quest;
                if (inProgress == null && quest.status == QuestStatus.InProgress) inProgress = quest;
            }
            return inProgress;
        }

        private static Transform FindNpc(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;

            foreach (NPCInteractable npc in FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None))
            {
                if (npc.NpcId == npcId) return npc.transform;
            }
            return null;
        }

        private static Transform FindNearestQuestGiver(PlayerQuestTracker player, Vector3 from)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;

            foreach (QuestGiver giver in FindObjectsByType<QuestGiver>(FindObjectsSortMode.None))
            {
                if (giver.GetMarker(player) != QuestMarkerType.Available) continue;
                Consider(giver.transform, from, ref best, ref bestDistance);
            }
            return best;
        }

        // Nearest living enemy still needed, or its spawner while they respawn
        private static Transform FindNearestEnemy(QuestInstance quest, Vector3 from)
        {
            EnemyTarget[] targets = quest.template.objective.enemyTargets;
            if (targets == null) return null;

            Transform best = null;
            float bestDistance = float.MaxValue;

            foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                if (!enemy.IsSpawned || enemy.IsDeadSynced || !IsNeeded(quest, enemy.Stats)) continue;
                Consider(enemy.transform, from, ref best, ref bestDistance);
            }
            if (best != null) return best;

            foreach (EnemySpawner spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
            {
                if (!IsNeeded(quest, spawner.SpawnedStats)) continue;
                Consider(spawner.transform, from, ref best, ref bestDistance);
            }
            return best;
        }

        private static bool IsNeeded(QuestInstance quest, EnemyStats stats)
        {
            if (stats == null) return false;

            EnemyTarget[] targets = quest.template.objective.enemyTargets;
            for (int i = 0; i < targets.Length; i++)
            {
                // Same name match as PlayerQuestTracker.ReportKill
                if (targets[i].enemy != null && targets[i].enemy.enemyName == stats.enemyName
                    && quest.GetKillProgress(i) < targets[i].amount)
                    return true;
            }
            return false;
        }

        private static Transform FindNearestBonfire(Vector3 from)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (Obrissom.Environment.Bonfire bonfire in FindObjectsByType<Obrissom.Environment.Bonfire>(FindObjectsSortMode.None))
                Consider(bonfire.transform, from, ref best, ref bestDistance);
            return best;
        }

        private static Transform FindNearestCollectable(QuestObjective objective, Vector3 from)
        {
            if (objective.itemTargets == null) return null;

            Transform best = null;
            float bestDistance = float.MaxValue;

            foreach (Collectable collectable in FindObjectsByType<Collectable>(FindObjectsSortMode.None))
            {
                if (!collectable.IsAvailable || collectable.Item == null) continue;

                foreach (ItemTarget target in objective.itemTargets)
                {
                    if (target.item != collectable.Item) continue;
                    Consider(collectable.transform, from, ref best, ref bestDistance);
                    break;
                }
            }
            return best;
        }

        private static void Consider(Transform candidate, Vector3 from, ref Transform best, ref float bestDistance)
        {
            float distance = (candidate.position - from).sqrMagnitude;
            if (distance >= bestDistance) return;

            bestDistance = distance;
            best = candidate;
        }

        private PlayerQuestTracker GetLocalPlayer()
        {
            if (_player != null) return _player;

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null) return null;

            return manager.LocalClient.PlayerObject.GetComponent<PlayerQuestTracker>();
        }

        // UI

        private void BuildUI()
        {
            var root = new GameObject("Quest Waypoint", typeof(RectTransform));
            _root = (RectTransform)root.transform;
            _root.SetParent(transform, false);
            _root.SetAsFirstSibling(); // menus and popups draw on top
            _root.sizeDelta = Vector2.zero;

            var arrow = new GameObject("Arrow", typeof(RectTransform));
            _arrow = (RectTransform)arrow.transform;
            _arrow.SetParent(_root, false);
            _arrow.sizeDelta = new Vector2(_arrowSize, _arrowSize);

            Image image = arrow.AddComponent<Image>();
            image.sprite = CreateArrowSprite();
            image.color = _color;
            image.raycastTarget = false;

            Outline outline = arrow.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            var text = new GameObject("Distance", typeof(RectTransform));
            text.transform.SetParent(_root, false);
            _distanceText = text.AddComponent<TextMeshProUGUI>();
            if (_font != null) _distanceText.font = _font;
            _distanceText.fontSize = _fontSize;
            _distanceText.color = _color;
            _distanceText.alignment = TextAlignmentOptions.Center;
            _distanceText.overflowMode = TextOverflowModes.Overflow;
            _distanceText.raycastTarget = false;
            _distanceText.rectTransform.sizeDelta = new Vector2(160f, _fontSize * 1.5f);
        }

        // White triangle pointing up, made in code so no sprite asset is needed
        private static Sprite CreateArrowSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                // Width shrinks from the base (bottom) to the tip (top)
                float halfWidth = (1f - (y + 0.5f) / size) * size * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float fromCenter = Mathf.Abs(x + 0.5f - size * 0.5f);
                    float alpha = Mathf.Clamp01(halfWidth - fromCenter + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
