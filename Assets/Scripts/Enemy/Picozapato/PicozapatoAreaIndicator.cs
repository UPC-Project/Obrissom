using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// Client-only ground telegraph for the Picozapato area attack.
    /// An outer ring marks the danger zone and an inner fill grows until it reaches the edge: that's the hit.
    /// Progress is computed from server time, so every client (and late joiners) sees the same remaining time.
    ///
    /// Custom prefab convention (same as the DPS magic ring): visuals authored with a diameter of 1 unit, facing +Z.
    /// Leave the parts empty to use the generated ring.
    /// </summary>
    public class PicozapatoAreaIndicator : MonoBehaviour
    {
        private const int FallbackSegments = 48;
        private const float FallbackOuterWidth = 0.08f;
        private const float FallbackFillWidth = 0.05f;
        private const float GroundProbeHeight = 0.5f;

        private static Material s_generatedMaterial;

        [SerializeField] private Transform _outerRing;
        [SerializeField] private Transform _fill;
        [Tooltip("Renderers tinted from base to warning color as the hit approaches.")]
        [SerializeField] private Renderer[] _tintedRenderers;
        [SerializeField] private string _colorProperty = "_BaseColor";
        [SerializeField] private float _groundOffset = 0.05f;

        [Tooltip("Progress (0–1) after which the indicator starts flashing the warning color.")]
        [SerializeField, Range(0f, 1f)] private float _warningThreshold = 0.75f;
        [SerializeField] private float _warningPulseFrequency = 12f;

        private enum Mode { Hidden, Telegraph, Impact }

        private Mode _mode = Mode.Hidden;
        private double _startServerTime;
        private float _duration;
        private float _impactEndTime;
        private float _diameter;
        private Color _baseColor;
        private Color _warningColor;

        private LineRenderer _outerLine;
        private LineRenderer _fillLine;
        private MaterialPropertyBlock _propertyBlock;
        private int _colorId;

        /// <summary>Creates the default indicator (two LineRenderer rings) when no prefab is configured.</summary>
        public static PicozapatoAreaIndicator CreateFallback(Material material)
        {
            var root = new GameObject("PicozapatoAreaIndicator");
            var indicator = root.AddComponent<PicozapatoAreaIndicator>();

            if (material == null)
            {
                if (s_generatedMaterial == null)
                {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader != null) s_generatedMaterial = new Material(shader);
                    else Debug.LogWarning("[PicozapatoAreaIndicator] No material for the generated indicator. Assign fallbackIndicatorMaterial in PicozapatoConfig.");
                }
                material = s_generatedMaterial; // Shared by every generated indicator
            }

            indicator._outerLine = CreateRing(root.transform, "Outer", material, FallbackOuterWidth);
            indicator._fillLine = CreateRing(root.transform, "Fill", material, FallbackFillWidth);
            indicator._outerRing = indicator._outerLine.transform;
            indicator._fill = indicator._fillLine.transform;
            return indicator;
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(_colorProperty);
            gameObject.SetActive(false);
        }

        public void ShowTelegraph(Vector3 center, float radius, double startServerTime, float duration,
                                  Color baseColor, Color warningColor, LayerMask groundMask)
        {
            _startServerTime = startServerTime;
            _duration = Mathf.Max(duration, 0.01f);
            _diameter = radius * 2f;
            _baseColor = baseColor;
            _warningColor = warningColor;
            _mode = Mode.Telegraph;

            PlaceOnGround(center, groundMask);
            SetDiameter(_outerRing, _outerLine, _diameter);

            gameObject.SetActive(true);
            Refresh();
        }

        public void PlayImpact(Vector3 center, float radius, float duration, Color warningColor, LayerMask groundMask)
        {
            // Late joiners may receive the impact without having seen the telegraph
            if (_mode == Mode.Hidden)
            {
                _diameter = radius * 2f;
                PlaceOnGround(center, groundMask);
                SetDiameter(_outerRing, _outerLine, _diameter);
            }

            _warningColor = warningColor;
            _mode = Mode.Impact;
            _impactEndTime = Time.time + duration;

            SetDiameter(_fill, _fillLine, _diameter);
            ApplyColor(warningColor);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _mode = Mode.Hidden;
            gameObject.SetActive(false);
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            switch (_mode)
            {
                case Mode.Telegraph:
                {
                    float progress = Mathf.Clamp01((float)((GetServerTime() - _startServerTime) / _duration));
                    SetDiameter(_fill, _fillLine, Mathf.Lerp(0.05f, 1f, progress) * _diameter);

                    Color color = _baseColor;
                    if (progress >= _warningThreshold)
                    {
                        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _warningPulseFrequency * Mathf.PI * 2f);
                        color = Color.Lerp(_baseColor, _warningColor, pulse);
                    }
                    ApplyColor(color);
                    break;
                }

                case Mode.Impact:
                {
                    float remaining = _impactEndTime - Time.time;
                    if (remaining <= 0f)
                    {
                        Hide();
                        return;
                    }

                    Color faded = _warningColor;
                    faded.a *= Mathf.Clamp01(remaining / 0.2f);
                    ApplyColor(faded);
                    break;
                }
            }
        }

        private void PlaceOnGround(Vector3 center, LayerMask groundMask)
        {
            // The center already comes snapped to the NavMesh; this only refines height and slope.
            // Starting just above it keeps the ray inside anyone standing there (rays ignore colliders they start in),
            // so it can't land on top of a player or enemy.
            Vector3 normal = Vector3.up;
            if (Physics.Raycast(center + Vector3.up * GroundProbeHeight, Vector3.down, out RaycastHit hit,
                                GroundProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                center = hit.point;
                normal = hit.normal;
            }

            transform.SetPositionAndRotation(center + normal * _groundOffset, Quaternion.FromToRotation(Vector3.forward, normal));
        }

        private void ApplyColor(Color color)
        {
            if (_outerLine != null)
            {
                _outerLine.startColor = _outerLine.endColor = color;
                Color fillColor = color;
                fillColor.a *= 0.7f;
                _fillLine.startColor = _fillLine.endColor = fillColor;
                return;
            }

            if (_tintedRenderers == null) return;
            foreach (Renderer tinted in _tintedRenderers)
            {
                if (tinted == null) continue;
                tinted.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_colorId, color);
                tinted.SetPropertyBlock(_propertyBlock);
            }
        }

        private static double GetServerTime() =>
            NetworkManager.Singleton != null ? NetworkManager.Singleton.ServerTime.Time : Time.timeAsDouble;

        private static LineRenderer CreateRing(Transform parent, string name, Material material, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.alignment = LineAlignment.TransformZ;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.positionCount = FallbackSegments;
            return line;
        }

        /// <summary>Generated rings rebuild their points (keeps line width constant), custom visuals are scaled.</summary>
        private static void SetDiameter(Transform part, LineRenderer line, float diameter)
        {
            if (line != null)
            {
                // Ring in local XY: the root's +Z faces the ground normal
                float radius = diameter * 0.5f;
                for (int i = 0; i < FallbackSegments; i++)
                {
                    float angle = i / (float)FallbackSegments * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
                }
                return;
            }

            if (part != null) part.localScale = Vector3.one * diameter;
        }
    }
}
